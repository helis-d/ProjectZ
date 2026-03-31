# Project Z Game Design Audit

Tarih: 2026-03-31

## Kisa Sonuc

Project Z iyi bir yon secmis: dusuk TTK, round tabanli ekonomi, objective baskisi ve hero farklilastirmasi ayni cati altinda birlestirilmeye calisiliyor. Fakat repo su an "bitmis rekabetci hero shooter" seviyesinde degil. Bugunku hali daha cok guclu bir GDD ustune kurulmus erken/orta asama vertical slice gibi duruyor.

En buyuk tasarim problemi fikir eksikligi degil, odak daginikligi. Oyun ayni anda cok fazla iddia tasiyor:

- Valorant benzeri round ekonomisi
- cok dusuk TTK
- canli mastery buff sistemi
- yuksek etkili ultiler
- birden fazla oyun modu

Bu kombinasyon dogru kuruldugunda fark yaratir. Yanlis sirayla yapildiginda ise rekabetci butunlugu bozar.

## Mevcut Durum Ozeti

Bugun repoda gercekten gorunen seyler:

- temel round state machine var
- ekonomi mantigi var
- plant/defuse akisi var
- hitscan ve server-authoritative combat omurgasi var
- HUD, scoreboard, buy menu gibi prototip UI katmani var
- FishNet ve Nakama entegrasyonu baslamis

Bugun eksik veya urunlesmemis gorunen seyler:

- aktif build icin tek sahne var: `Assets/Scenes/SampleScene.unity`
- roster 13 hero olarak anlatiliyor ama author edilmis tek hero verisi `Assets/Volt_HeroData.asset`
- o hero datasinda bile `ultimateAbilityPrefab` bagli degil
- map/data-driven objective sistemi var ama buy zone tarafi halen TODO
- lobby to match flow tamam degil, menu tarafi halen prototip

Sonuc: proje "tam 5v5 hero shooter" degil, "cekirdek loop'u kurulmaya calisan prototip" olarak ele alinmali.

## Guclu Yonler

### 1. Core fantasy net
Oyun ne olmak istedigini biliyor: rekabetci, hizli, sert, bilgi odakli bir tactical hero shooter.

### 2. Sistemler birbirine teorik olarak bagli
Round, ekonomi, objective, mastery ve ultimate sistemleri ayri ayri dusunulmemis. Bu iyi bir tasarim refleksi.

### 3. Rekabetci omurgaya dogru teknik secimler yapilmis
Server authority, rollback, hit validation ve objective state machine gibi secimler dogru yonde.

### 4. Oyuncuya okunabilir geri bildirim verme niyeti var
HUD, crosshair bloom, scoreboard gizlilik kurallari ve objective zamanlayicilari dogru urun dusuncesine isaret ediyor.

## Buyuk Problemler

### 1. Vaat edilen urun ile mevcut oyun kapsami ayni seviyede degil
README ve GDD tam roster, tam mod paketi ve ileri seviye rekabetci paket anlatiyor. Repo ise daha dar bir oynanabilir dilime sahip.

Bu fark tek basina problem yaratir cunku ekip yanlis onceliklendirme yapmaya baslar: once urun kimligini degil, once kapsam illuzyonunu buyutmus olur.

### 2. Mastery sistemi rekabetci dengeyi bozma riski tasiyor
Dusuk TTK'li oyunda kazanan oyuncuya daha hizli ADS, reload, hareket ve fire rate vermek snowball uretir.

Bu sistem:

- iyi oynayani odullendiriyor
- ama ayni zamanda geri dusen oyuncunun geri donus sansini azaltabiliyor

Bu tasarim arcade shooter'da cok iyi calisabilir. Siki rekabetci tactical shooter'da dikkatli sinirlanmazsa adalet algisini zedeler.

### 3. Ultimate tasarimlari yer yer fazla kaotik
Ozellikle global veya HUD bozan ultler, taktik shooter okunurlugunu kolayca kirar.

En riskli tipler:

- tum HUD'u bozan etkiler
- gorus alanini sert kapatan global etkiler
- rakibin temel bilgi alma araclarini gecici tamamen iptal eden durumlar

Bu tip ultler kisa vadede havali gorunur ama uzun vadede oyuncuya "yenildim cunku oynanis bozuldu" hissi verebilir.

### 4. Mod kimligi net degil
Ranked, Fast Fight, Duel Chaos ve Solo Tournament ayni anda tasarlaniyor; fakat temel urun loop'u once tek modda kusursuza yaklasmamis gorunuyor.

Rekabetci oyunda ana mod oturmadan yan modlar eklemek, denge ve tempo problemlerini carpani buyutur.

### 5. Kod ve dokuman arasinda urun karari seviyesinde celiskiler var
Bu kisim tasarim acisindan kritik:

- README Fast Fight'i 5 round diyor, kod 10 round yapiyor
- README Ranked icin overtime diyor, kodda belirgin overtime akisi yok
- Ranked "13 round" dili ile koddaki `WinsRequired = 7` ayni urun anlatimini vermiyor

Bu tip farklar ekipte su soruyu dogurur: gercek oyun kurali hangisi?

## Profesyonel Degerlendirme

Project Z'nin cekirdek fikri zayif degil. Hatta dogru sadelestirilirse guclu olabilir. Sorun, oyunun su an "kimlik" yerine "ozellik yogunlugu" ile buyumeye calismasi.

Ben olsaydim urunu su cizgiye cekerim:

- once sadece tek ana mod
- once sadece 2 site veya 1 net objective layout
- once sadece 3-4 hero
- mastery sistemini ya kapatirim ya da cok yumusatirim
- okunurlugu bozan global ultileri gecici olarak asagi cekerim

Bunun nedeni basit:
rekabetci bir oyunda once adalet, okunurluk ve round kalitesi kazanir. Sonra kaos, varyasyon ve spektakl eklenir.

## Ilk Once Yapilmasi Gereken 5 Sey

### 1. Tek urun tanimi sec
Oyun once hangi sey olacak kararini kilitle:

- rekabetci tactical hero shooter mi
- yoksa mastery ve kaos odakli hibrit arena shooter mi

Su an iki yone birden cekiliyor.

### 2. Tek gercek oyun modu belirle
Ilk shipping hedefi sadece Ranked benzeri ana mod olsun.

Cikartilacak veya beklemeye alinacak:

- Duel Chaos
- Solo Tournament
- fazla hizli varyasyonlar

### 3. Mastery sistemini yeniden sinirla
Onerim:

- mastery ya sadece gorsel/feedback odulu olsun
- ya da cok kucuk handling bonuslari versin
- fire rate ve hareket hizi gibi direkt duel avantajlarini azalt

Tactical oyunda "kazanan daha da kolay kazansin" hissi tehlikelidir.

### 4. Hero roster'i kucult ve tamamla
13 eksik hero yerine 3 veya 4 tam hazir hero daha degerlidir.

Minimum hedef:

- her hero icin tam data asset
- bagli ultimate prefab
- net rol
- karsi oyun imkani
- okunur VFX/SFX

### 5. GDD ile kodu ayni urun gercegine getir
Tek bir source of truth secilmeli.

Temizlenmesi gereken ilk basliklar:

- round sayisi
- kazanma kosulu
- overtime var mi yok mu
- Fast Fight'in kimligi
- pistol round kurallari

## Yol Haritasi

### Asama 1 - Vertical Slice'i kilitle

- tek harita
- tek ana mod
- 3-4 hero
- plant/defuse loop'u
- ekonomi + HUD + scoreboard okunurlugu

Basari olcutu:
oyuncu 5 round ust uste "bu oyun ne olmak istiyor" sorusunu sormadan oynayabilmeli.

### Asama 2 - Rekabetci dengeyi kur

- mastery etkilerini sinirla
- en rahatsiz edici ultileri yeniden tasarla
- info clarity ve counterplay ekle

Basari olcutu:
oyuncu oldugunde nedeni anlayabilmeli; "sistem beni oyundan kopardi" hissi olusmamali.

### Asama 3 - Kapsami buyut

- ek modlar
- daha fazla hero
- backend ve progression derinligi

Basari olcutu:
ana mod zaten gucluyken ekstra modlar onu desteklemeli, dagitmamali.

## Son Verdict

Project Z'nin en guclu yani cesur bir urun hayali olmasi.
En buyuk riski ise bu hayalin bugunku prototip kapasitesinden daha hizli buyumesi.

Bu proje kurtarilacak veya toparlanacak bir fikir degil; dogru kisitlanirsa guclu bir urune donusebilir.
Ama bunun icin once daha fazla ozellik degil, daha sert odak gerekir.
