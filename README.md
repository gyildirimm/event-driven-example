# E-Ticaret Sistemimiz Nasıl Çalışıyor?

Bu projede modern bir e-ticaret sisteminin temel işleyişini gösteren **otomatik sipariş-stok-bildirim süreci** geliştirdik. Sistemimiz müşteri deneyimini artırırken operasyonel verimliliği de sağlıyor.

## 🎯 Ana Sistem Akışı

### 1️⃣ Sipariş Alma Süreci

- **Müşteri Siparişi**: Müşteri ürün seçer ve sipariş oluşturur
- **Otomatik Stok Kontrolü**: Sistem hemen stok durumunu kontrol eder
- **Rezervasyon**: Stok mevcutsa otomatik olarak rezerve edilir
- **Anlık Geri Bildirim**: Müşteri 2-3 saniye içinde onay alır

### 2️⃣ Stok Yönetimi

- **Gerçek Zamanlı Stok Takibi**: Her sipariş anında stoktan düşülür
- **Otomatik Rezervasyon**: Ürünler sipariş onaylanana kadar bekletilir
- **Stok Güvenliği**: Eş zamanlı siparişlerde çakışma olmaz

### 3️⃣ Müşteri Bildirimleri

- **Sipariş Onayı**: E-posta ve SMS ile anlık bilgilendirme
- **Durum Güncellemeleri**: Sipariş durumu değişikliklerinde bildirim

## 🏗️ Sistem Mimarisi Özeti

### Microserviş Yaklaşımı

Sistem 3 ana bileşenden oluşur:

- **Sipariş Servisi**: Müşteri siparişlerini yönetir
- **Stok Servisi**: Ürün stoklarını takip eder  
- **Bildirim Servisi**: E-posta/SMS gönderimlerini yapar

### Otomatik İletişim

- Servisler birbirleriyle **otomatik mesajlaşarak** çalışır
- İnsan müdahalesi olmadan kararlar alır
- Hata durumlarında kendini düzeltir

### Ölçeklenebilir Altyapı

- Docker container'ları ile **ölçeklenebilir altyapı**
- Her servis bağımsız olarak güncellenebilir
- Yeni özellikler mevcut sistemi bozmaz

## 🛠️ Kullanılan Teknolojiler

### Ana Framework ve Dil

- **.NET 8**: Modern ve performanslı uygulama geliştirme
- **C#**: Güvenli ve tip-safe programlama dili

### Veri Tabanı ve ORM

- **PostgreSQL**: Güvenilir ve ölçeklenebilir ilişkisel veritabanı
- **Entity Framework Core 8.0**: Modern veri erişim teknolojisi

### Mesajlaşma ve Event-Driven Mimari

- **RabbitMQ**: Güvenilir mesaj kuyruğu sistemi
- **Custom Event System**: Servisler arası otomatik iletişim

### API ve Gateway

- **YARP (Yet Another Reverse Proxy)**: Microsoft'un modern API Gateway çözümü
- **REST API**: Standart web API protokolü
- **Swagger/OpenAPI**: API dokümantasyonu

### Container ve DevOps

- **Docker**: Container teknolojisi
- **Docker Compose**: Multi-container orkestrasyon

### Monitoring ve Logging

- **Seq**: Gerçek zamanlı log analizi ve görselleştirme
- **Serilog**: Yapılandırılabilir logging framework

### Validation ve Mapping

- **FluentValidation**: Güçlü veri doğrulama sistemi

### Mimari Desenler

- **Clean Architecture**: Katmanlı ve sürdürülebilir kod yapısı
- **CQRS (Command Query Responsibility Segregation)**: Okuma/yazma işlemlerinin ayrılması
- **Domain-Driven Design (DDD)**: İş mantığı odaklı tasarım
- **Event-Driven Architecture**: Olay tabanlı sistem mimarisi
- **Microservices**: Bağımsız servis mimarisi

## 🚀 Çalıştırma ve Yönetim

### Basit Başlatma

```bash
# Tek komutla tüm sistem ayağa kalkar
docker compose up -d
```

### Monitoring ve İzleme

- **Seq Dashboard**: Sistemin gerçek zamanlı durumu (<http://localhost:9180>)
- **RabbitMQ Panel**: Mesaj kuyruğu takibi (<http://localhost:15672>)
- **Database Admin**: Veri tabanı yönetimi (<http://localhost:7010>)
- **API Gateway**: Tüm servislere tek noktadan erişim (<http://localhost:6262>)

### Sistem Sağlığı

- Otomatik hata yakalama ve raporlama

## 📊 Demo Senaryoları

### Başarılı Sipariş Akışı

1. Müşteri 2 adet "Laptop" sipariş eder
2. Sistem stokta 5 adet olduğunu görür
3. 2 adet rezerve eder (kalan 3)
4. Sipariş onaylanınca stoktan düşer

### Stok Yetersizliği Senaryosu

1. Müşteri 10 adet ürün sipariş eder
2. Stokta sadece 3 adet var
3. Sistem otomatik olarak siparişi reddeder
4. Sipariş iptal edilir

---

## 💼 Genel Özet

Bu sistem modern e-ticaretin temel gereksinimlerini karşılayan, **tamamen otomatik** çalışan bir platformdur.
