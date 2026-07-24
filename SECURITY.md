# Güvenlik Politikası

## Desteklenen Sürüm

Carlens AI aktif geliştirme aşamasındadır. Güvenlik düzeltmeleri `main`
dalının güncel sürümüne uygulanır.

## Güvenlik Açığı Bildirme

Bir güvenlik açığı bulursanız lütfen herkese açık issue açmayın. GitHub
deposundaki **Security > Advisories > Report a vulnerability** seçeneğini
kullanarak özel bildirim gönderin.

Bildirimde mümkünse şu bilgileri paylaşın:

- Etkilenen bileşen ve sürüm
- Sorunu tekrar oluşturma adımları
- Beklenen ve gerçekleşen davranış
- Olası etki
- Varsa önerilen çözüm

Geçerli bildirimler incelenir, etkisi doğrulanır ve düzeltme yayımlanana kadar
ayrıntılar özel tutulur.

## Gizli Bilgiler

- API anahtarlarını, parolaları, bağlantı bilgilerini veya `.env` dosyalarını
  commit etmeyin.
- OpenAI anahtarı yalnızca AiWorker çalışma zamanına verilmelidir.
- Yerel geliştirmede .NET User Secrets, dağıtım ortamında bir secret manager
  veya güvenli ortam değişkenleri kullanılmalıdır.
- Yanlışlıkla yayımlanan bir gizli bilgi derhal iptal edilip yenilenmelidir.

## Güvenli Dağıtım

Kök dizindeki Docker Compose yapılandırması yerel geliştirme içindir. Canlı
ortamda TLS, özel ağ, kimlik doğrulama, merkezi secret manager, kalıcı dağıtık
oturum ve gözlemlenebilirlik ayrıca yapılandırılmalıdır.
