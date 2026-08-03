# Stateless Çalışma Modeli

Carlens AI'ın Web ve API süreçleri yatay ölçeklenebilir olacak şekilde tasarlanır. Bir HTTP isteği herhangi bir replikaya ulaşabilir; process yeniden başladığında kalıcı kullanıcı veya iş verisi kaybolmamalıdır.

## Durum Sahipliği

| Durum | Sahibi | Gerekçe |
|---|---|---|
| Araç ilanları, analizler ve yüklenen görseller | PostgreSQL | Kalıcı iş verisinin tek doğruluk kaynağıdır. |
| Analiz istekleri | RabbitMQ | HTTP isteğinden bağımsız, asenkron iş akışını taşır. |
| Aynı ilanın tekrar analiz edilmesini önleyen rezervasyon | Redis | Replikalar arasında paylaşılan atomik ve süreli kilittir. |
| Web oturumu ve erişim izinleri | Redis | Kullanıcı aynı replikaya bağlı kalmadan istek gönderebilir. |
| Data Protection anahtar halkası | Redis | Cookie ve antiforgery verileri bütün Web replikalarında çözülebilir. |
| Analiz oluşturma kotası | Redis | Lua script ile atomik artırılır ve süre sonunda otomatik silinir. |

## Process İçinde Kalabilen Durum

Aşağıdaki nesneler kalıcı iş verisi değildir ve process yeniden başladığında güvenle yeniden oluşturulabilir:

- Playwright browser örneği ve eşzamanlılık semaforları
- HTTP, PostgreSQL, Redis ve RabbitMQ bağlantı havuzları
- Dependency Injection container'ındaki singleton servis örnekleri
- React bileşenlerinin tarayıcıdaki geçici arayüz durumu

Uygulama container dosya sistemine kullanıcı veya iş verisi yazmaz. Kalıcı veriler dış servislerde tutulduğu için Web ve API replikaları yeniden başlatılabilir, değiştirilebilir veya paralel çalıştırılabilir.

## Operasyonel Sınırlar

- Production ortamında istemci IP'si yalnızca güvenilen reverse proxy/load balancer üzerinden `Forwarded Headers` yapılandırmasıyla alınmalıdır.
- Worker shutdown sırasında yeni RabbitMQ teslimatlarını durdurur ve devam eden mesajı yapılandırılmış süre içinde tamamlamaya çalışır; süre aşılırsa mesaj ack edilmeden kanal kapanır ve broker tarafından yeniden kuyruğa alınır.
- RabbitMQ publisher mesaj kalıcılığı, retry ve dead-letter queue ayrı Worker güvenilirliği çalışmalarında ele alınacaktır.
- Production Redis bağlantısı TLS kullanmalı; Data Protection anahtarları Key Vault anahtarıyla şifrelenmelidir.
