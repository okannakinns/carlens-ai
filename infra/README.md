# Carlens AI Azure Infrastructure

Bu dizin, Carlens AI'ın staging ve production Azure altyapısını Bicep ile
tanımlar. Altyapı ile uygulama dağıtımı iki ayrı giriş noktasına ayrılmıştır:

- `main.bicep`: Ortamın uzun ömürlü temel kaynaklarını oluşturur.
- `apps.bicep`: Aynı immutable image etiketiyle API, Web ve Worker revision'larını
  dağıtır.
- `migration-job.bicep`: API image'ındaki EF Core migration bundle'ını tek
  replica ve manuel tetiklemeyle çalıştıran Container Apps Job'u tanımlar.

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

`postgres-connection-string`, Azure PostgreSQL FQDN'ini kullanmalı ve TLS
sertifika doğrulamasını `SSL Mode=VerifyFull` ile zorunlu tutmalıdır. Kerberos
kullanılmayan bu parola tabanlı bağlantıda `GSS Encryption Mode=Disable`, minimal
Linux image'larında gereksiz GSSAPI denemesini de engeller.

## Yerel Doğrulama

[Bicep CLI](https://github.com/Azure/bicep/releases) `v0.46.1` ile:

```powershell
$env:CARLENS_POSTGRES_ADMIN_PASSWORD = "yalnızca-yerel-doğrulama-parolası"
$env:CARLENS_IMAGE_TAG = "sha-0123456789abcdef0123456789abcdef01234567"

bicep lint infra/main.bicep
bicep lint infra/apps.bicep
bicep lint infra/migration-job.bicep
bicep build infra/main.bicep
bicep build infra/apps.bicep
bicep build infra/migration-job.bicep
bicep build-params infra/environments/staging.foundation.bicepparam
bicep build-params infra/environments/production.foundation.bicepparam
bicep build-params infra/environments/staging.apps.bicepparam
bicep build-params infra/environments/production.apps.bicepparam
bicep build-params infra/environments/staging.migration.bicepparam
bicep build-params infra/environments/production.migration.bicepparam
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

## Staging CD

`.github/workflows/staging.yml`, `main` branch'ine yapılan her push'ta staging
dağıtımını çalıştırır. Aynı commit için zorunlu CI kontrollerinin tamamlanmasını
bekler; API, Web ve Worker image'larını üretip Trivy ile tarar, ACR'a gönderir ve
ilgili image sürümünü yazma/silme işlemlerine karşı kilitler. Ardından migration
job sırasıyla çalıştırılır, uygulamalar dağıtılır ve dış Web adresi üzerinden
live, ready, SPA, güvenlik başlığı ve API gateway smoke testleri uygulanır.

Workflow, repository değişkeni `AZURE_DEPLOYMENTS_ENABLED` değeri `true`
olmadıkça bilinçli olarak skip edilir. Böylece örnek repository fork'ları veya
henüz Azure kurulumu yapılmamış ortamlar yanlışlıkla kaynak oluşturamaz.

GitHub'da `staging` Environment'ını yalnızca protected branch deployment'larına
açın ve aşağıdaki environment variable'larını tanımlayın:

| Variable | Açıklama |
|---|---|
| `AZURE_CLIENT_ID` | Staging deployment kimliğinin application/client ID değeri |
| `AZURE_TENANT_ID` | Microsoft Entra tenant ID değeri |
| `AZURE_SUBSCRIPTION_ID` | Staging kaynaklarının bulunduğu subscription ID değeri |
| `AZURE_RESOURCE_GROUP` | Varsayılan kurulumda `rg-carlens-staging` |

Kimlik doğrulama client secret kullanmaz. Azure deployment kimliğinde GitHub
OIDC için aşağıdaki subject'e sahip federated credential tanımlanır:

```text
repo:okannakinns/carlens-ai:environment:staging
```

Deployment kimliği staging resource group üzerinde `Contributor`, staging ACR
üzerinde `AcrPush` rollerine sahip olmalıdır. Foundation kaynakları ve Key Vault
secret sözleşmesi hazırlandıktan sonra repository variable'ını etkinleştirin:

```text
AZURE_DEPLOYMENTS_ENABLED=true
```

Migration, yeni revision'lardan önce uygulanır. Bu nedenle production'a taşınan
schema değişiklikleri expand/contract yaklaşımıyla geriye uyumlu tutulmalıdır;
veritabanı migration'ları uygulama rollback'i sırasında otomatik geri alınmaz.

## Production Blue-Green CD

`.github/workflows/production.yml` yalnızca `main` branch'i üzerinden manuel
olarak çalıştırılır ve protected `production` GitHub Environment onayı bekler.
Workflow'a production'a alınacak, staging deployment'ı başarıyla tamamlanmış tam
40 karakterlik commit SHA'sı verilir. Pipeline zorunlu kalite kontrollerini ve o
commit'e ait başarılı staging deployment kaydını yeniden doğrular.

Production image'ları tekrar build edilmez. Staging ACR'daki yazma ve silmeye
karşı kilitli API, Web ve Worker manifestleri digest üzerinden ayrı production
ACR'a import edilir; hedef digest'in kaynakla birebir aynı olduğu doğrulanıp hedef
etiket de kilitlenir. Böylece staging'de test edilen artifact production'a taşınır.

Migration job yeni revision'lardan önce çalışır. Mevcut production ortamında yeni
API ve Web revision'ları `candidate` etiketi ve `%0` trafikle oluşturulur. Aday
revision FQDN'i üzerinden smoke test geçtikten sonra trafik `%5`, `%25`, `%50` ve
`%100` olarak aktarılır. Her aşamada gözlem süresi, revision readiness kontrolü ve
tekrarlı production smoke testi bulunur. Herhangi bir kontrol başarısız olursa API
ve Web trafiği önceki stable revision'lara döner, revision etiketleri onarılır ve
Worker önceki image'a geri alınır. Rollback sonrasında yeniden smoke test yapılır.

Worker, iki RabbitMQ consumer sürümünün aynı anda çalışmasını önlemek için HTTP
trafiği `%100` adaya geçmeden güncellenmez ve `Single` revision modunda tutulur.
İlk production kurulumu için eski revision bulunmadığından workflow kontrollü bir
bootstrap uygular; üç uygulamayı readiness ve smoke testlerden sonra `%100`
trafikle devreye alır.

GitHub'da `production` Environment'ını required reviewer ve yalnızca protected
branch deployment politikasıyla yapılandırın. Aşağıdaki environment variable'ları
tanımlanmalıdır:

| Variable | Açıklama |
|---|---|
| `AZURE_CLIENT_ID` | Production deployment kimliğinin application/client ID değeri |
| `AZURE_TENANT_ID` | Microsoft Entra tenant ID değeri |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID değeri |
| `AZURE_RESOURCE_GROUP` | Varsayılan kurulumda `rg-carlens-production` |
| `AZURE_STAGING_RESOURCE_GROUP` | Varsayılan kurulumda `rg-carlens-staging` |

Production deployment kimliği için client secret yerine aşağıdaki GitHub OIDC
subject'ine sahip federated credential kullanılır:

```text
repo:okannakinns/carlens-ai:environment:production
```

Kimlik production resource group üzerinde `Contributor`, staging ACR üzerinde
`AcrPull` ve production ACR üzerinde `AcrPush` rollerine sahip olmalıdır. GitHub
Actions'ta **Production Blue-Green Deployment** workflow'unu `main` üzerinden
çalıştırıp başarılı staging commit SHA'sını ve gözlem süresini seçin.

`AZURE_DEPLOYMENTS_ENABLED=true` yalnızca iki Azure ortamı, Key Vault secret'ları,
OIDC federated credential'ları ve roller hazırlandıktan sonra ayarlanmalıdır.
Migration'lar expand/contract yaklaşımıyla geriye uyumlu olmalıdır; uygulama
rollback'i veritabanı migration'ını otomatik olarak geri almaz.
