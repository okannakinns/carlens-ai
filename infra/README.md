# Carlens AI Azure Infrastructure

Bu dizin, Carlens AI'ın staging ve production Azure altyapısını Bicep ile
tanımlar. Altyapı ile uygulama dağıtımı iki ayrı giriş noktasına ayrılmıştır:

- `main.bicep`: Ortamın uzun ömürlü temel kaynaklarını oluşturur.
- `apps.bicep`: Aynı immutable image etiketiyle API, Web ve Worker revision'larını
  dağıtır.

Bu ayrım, henüz ACR içinde image bulunmadan temel altyapının kurulabilmesini ve
uygulama revision'larının altyapıdan bağımsız olarak geri alınabilmesini sağlar.

## Mimari

```text
Internet
   |
   v
Carlens Web (external ingress)
   |
   v
Carlens API (internal ingress)
   |              |
   v              v
PostgreSQL      RabbitMQ (managed AMQPS)
   ^              |
   |              v
   +--------- Carlens AiWorker
                    |
                    v
                  OpenAI

API / Web / Worker ---> Azure Managed Redis
API / Web / Worker ---> Application Insights + Log Analytics
```

PostgreSQL ve Azure Managed Redis public internete kapalıdır. Container Apps
ortamına bağlı VNet içindeki private endpoint ve private DNS zone üzerinden
erişilir. Web dış ingress'e, API yalnızca iç ingress'e sahiptir; Worker ingress
açmaz. API ve Web, Container Apps ingress'inin eklediği forwarded header'ları
işleyerek özgün HTTPS şemasını ve istemci IP bilgisini korur.

## Kaynaklar

| Kaynak | Amaç |
|---|---|
| Azure Container Apps | API, Web ve Worker için revision tabanlı çalışma ortamı |
| Azure Container Registry | Rootless production image'larının immutable Git SHA etiketiyle saklanması |
| PostgreSQL Flexible Server 18 | İlişkisel uygulama verisi ve EF Core migration hedefi |
| Azure Managed Redis | Idempotency, session, rate limit ve Data Protection key ring |
| Azure Key Vault | Uygulama secret'larının kaynak kod ve deployment çıktılarından ayrılması |
| Application Insights | OpenTelemetry trace, metric ve log korelasyonu |
| Log Analytics | Container Apps ortam logları ve merkezi sorgulama |
| User-assigned managed identities | Her uygulamaya ayrı ACR Pull ve Key Vault Secret User yetkisi |

RabbitMQ, bu şablonda Azure üzerinde sanal makine olarak kurulmaz. Production
ortamında SLA, yedekleme ve TLS yöneten bir RabbitMQ sağlayıcısının `amqps://`
bağlantısı Key Vault'a eklenir. Worker, kuyruk uzunluğuna göre KEDA ile sınırlı
biçimde ölçeklenir.

## Ortam Farkları

| Ayar | Staging | Production |
|---|---:|---:|
| PostgreSQL | Burstable, 32 GiB, HA kapalı | General Purpose, 64 GiB, zone-redundant HA |
| PostgreSQL yedek saklama | 7 gün | 14 gün |
| Azure Managed Redis | Balanced B0 | Balanced B1 |
| Container Apps revision modu | Single | Multiple |
| API / Web minimum replica | 1 / 1 | 2 / 2 |
| Zone redundancy | Kapalı | Açık |
| Key Vault purge protection | Kapalı | Açık |
| Log saklama | 30 gün | 90 gün |

Production parametreleri kesintisiz revision geçişini gösterecek şekilde
hazırlanmıştır. Gerçek trafik aktarımı ve rollback, production CD workflow'unda
yönetilir.

## Secret Sözleşmesi

Repository içinde gerçek secret bulunmaz. Foundation deployment yalnızca
`CARLENS_POSTGRES_ADMIN_PASSWORD` ortam değişkenini okur. CD süreci aşağıdaki
değerleri Key Vault secret'ları olarak oluşturur:

| Key Vault secret | Kullanan servis |
|---|---|
| `postgres-connection-string` | API, Worker ve migration job |
| `redis-connection-string` | API, Web ve Worker |
| `rabbitmq-uri` | API ve Worker; Worker KEDA ölçek kuralı |
| `internal-api-key` | API ve Web |
| `openai-api-key` | Yalnızca Worker |

Container Apps, secret değerlerini Bicep parametresi olarak almaz. Key Vault
referanslarını kendi user-assigned managed identity'siyle çözer.

## Yerel Doğrulama

[Bicep CLI](https://github.com/Azure/bicep/releases) `v0.46.1` ile:

```powershell
$env:CARLENS_POSTGRES_ADMIN_PASSWORD = "yalnızca-yerel-doğrulama-parolası"
$env:CARLENS_IMAGE_TAG = "sha-0123456789abcdef0123456789abcdef01234567"

bicep lint infra/main.bicep
bicep lint infra/apps.bicep
bicep build infra/main.bicep
bicep build infra/apps.bicep
bicep build-params infra/environments/staging.foundation.bicepparam
bicep build-params infra/environments/production.foundation.bicepparam
bicep build-params infra/environments/staging.apps.bicepparam
bicep build-params infra/environments/production.apps.bicepparam
```

GitHub Actions aynı kontrolleri checksum ile doğrulanmış Bicep binary'siyle
çalıştırır. Derlenmiş ARM şablonlarını artifact olarak saklar; secret içerebilen
derlenmiş parametre dosyalarını hiçbir zaman yüklemez.

## Dağıtım Sırası

1. Foundation şablonunu ilgili ortam parametreleriyle dağıtın.
2. PostgreSQL, Redis, RabbitMQ, dahili API ve OpenAI secret'larını Key Vault'a
   güvenli deployment ortamından ekleyin.
3. API, Web ve Worker image'larını aynı `sha-<40 hex>` etiketiyle ACR'a gönderin.
4. EF Core migration job'ını çalıştırın.
5. `apps.bicep` ile revision'ları dağıtın.
6. Readiness ve smoke testleri geçtikten sonra trafiği aktarın.

Planlanan staging CD bu akışı otomatik uygulayacak. Production CD ise GitHub
Environment onayı, kademeli trafik aktarımı ve başarısız smoke testte otomatik
rollback ekleyecek.
