[English](README.md) · [中文](README.zh.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [বাংলা](README.bn.md) · [Português](README.pt.md) · [Русский](README.ru.md) · **Bahasa Indonesia**

# IPTzTray

**Zona waktu Windows dan DNS mengikuti IP publik, juga saat VPN menyala.**

Nama: IP + Tz (time zone, zona waktu) + Tray (area notifikasi Windows).

## Singkatnya

IPTzTray mengatur zona waktu Windows menurut IP publik, yaitu alamat yang benar-benar dipakai lalu lintas untuk keluar. Nyalakan VPN dari negara lain, zona waktu berganti ke negara keluar. Kembali ke IP rumah, zona waktu kembali menjadi zona rumah. Jika diinginkan, program juga menutup kebocoran DNS di luar terowongan, yang membuat penyedia internet melihat nama situs saat VPN menyala.

Yang berubah hanya zona waktu; jam itu sendiri (waktu UTC) tidak digeser. Layar menampilkan waktu lokal yang lain, dan situs, program, serta layanan menampilkan waktu yang sama.

Secara opsional, dan ini mati, program juga mengatur negara Windows menurut negara IP dan mengembalikan negara Anda saat pengaturan itu dimatikan. Bahasa tampilan dan wilayah akun Microsoft tidak berubah.

Dengan begitu program menghapus dua tanda VPN yang mencolok: waktu lokal yang tidak cocok dengan IP, dan kueri DNS yang lewat di luar terowongan. Tanda lain, misalnya IP pusat data atau bahasa sistem, tidak diubahnya.

Program melihat IP seluruh sistem. Proxy yang diatur hanya di browser tidak terdeteksi.

Gratis untuk penggunaan apa pun. Syarat untuk fork ada di bagian “Lisensi dan nama”.

## Untuk apa

- Situs, program, dan layanan menampilkan waktu rumah, padahal lalu lintas sudah keluar dari negara lain.
- VPN sudah mati, tetapi zona waktu tetap asing.
- Windows mengatur zona waktu menurut jaringan Wi‑Fi terdekat, bukan menurut IP yang dilalui lalu lintas.
- Saat VPN menyala, Windows tetap bertanya ke DNS router. Kueri lewat di luar terowongan, dan penyedia melihat nama situs.

Tiga poin pertama diselesaikan dengan penyesuaian zona waktu, poin keempat dengan perlindungan kebocoran DNS.

- Kemungkinan ini juga dapat membantu terhadap pemblokiran akun Claude (Anthropic) dan vendor lain untuk pengembangan agen AI.

## Cara mulai

Windows 10 atau 11, 64-bit. Tidak perlu instalasi: program berupa satu file, [IpTzTray.exe](https://github.com/kubsite1/iptztray/releases/latest/download/IpTzTray.exe), paling besar 250 KB. Semua versi ada di halaman [Releases](https://github.com/kubsite1/iptztray/releases).

1. Unduh dan jalankan `IpTzTray.exe`.
2. Izinkan hak administrator: tanpa itu Windows tidak mengizinkan zona waktu diubah.
3. Ikon muncul di area notifikasi; selanjutnya zona waktu menyesuaikan sendiri.

Saat pertama kali dijalankan, program mengingat zona waktu saat ini sebagai zona rumah dan menyalakan mulai otomatisnya saat masuk Windows. Setiap kali dijalankan dengan hak administrator, program mematikan pengaturan zona waktu otomatis Windows (layanan `tzautoupdate`); jika tidak, Windows mengatur zona waktu menurut Wi‑Fi terdekat dan nilainya berbeda dengan program.

Jika hak ditolak, ikon tetap muncul, tetapi zona waktu tidak berubah. Di menu tetap ada “Jalankan sebagai administrator”.

Saat keluar dari program dan saat sesi Windows berakhir (keluar akun atau mematikan komputer), zona waktu rumah dikembalikan, meski ganti otomatis sedang dijeda. Ini bisa dimatikan di pengaturan.

## Ikon

Ikon menampilkan selisih zona waktu Windows saat ini: `+3`, `-5`, `+5:30`.

<p>
<img src="screenshots/icon-match.png" width="56" alt="Hijau">
<img src="screenshots/icon-mismatch.png" width="56" alt="Oranye">
<img src="screenshots/icon-wait.png" width="56" alt="Abu-abu">
<img src="screenshots/icon-error.png" width="56" alt="Merah">
</p>

- **Hijau** — zona waktu Windows cocok dengan zona waktu IP.
- **Oranye** — tidak cocok: VPN sudah menyala tetapi zona waktu masih yang lama, atau ganti otomatis sedang dijeda.
- **Abu-abu** — IP belum diperiksa.
- **Merah** — IP atau zona waktu tidak dapat diketahui. Zona waktu tidak berubah.

Klik dua kali ikon untuk memeriksa sekarang. Salinan kedua program tidak berjalan: yang pertama tetap di area notifikasi.

## Kebocoran DNS

Pemeriksaan mati secara bawaan. Nyalakan di pengaturan.

Program memeriksa apakah Windows bertanya ke DNS tidak hanya lewat terowongan, tetapi juga lewat adapter aktif lain, biasanya router. Adapter virtual, Bluetooth, dan TAP tidak ikut diperiksa. Pemeriksaannya berupa satu kueri nama acak `.invalid` ke DNS tersebut. Jika DNS itu menjawab saat pencarian DNS paralel Windows menyala, nama situs bisa keluar di luar VPN.

![Jendela kebocoran DNS](screenshots/dns.png)

Jika kebocoran ditemukan, program menampilkan jendela ini. Menutup jendela dan “Nanti saja” tidak mengubah apa pun. “Perbaiki”, selama VPN menyala:

- mematikan pencarian DNS paralel di Windows;
- mengingat DNS adapter samping dan menggantinya dengan DNS terowongan;
- memblokir kueri keluar ke DNS lama dan ke DNS publik yang dikenal, termasuk DNS-over-TLS dan DNS-over-HTTPS;
- mengarahkan semua nama ke DNS terowongan.

Di jaringan rumah semuanya kembali seperti semula.

Mode:

- **Jangan periksa** — mencabut perlindungan yang sudah aktif dan mengembalikan pengaturan sebelumnya.
- **Sekali saat mulai** — bertanya hanya jika ada kebocoran saat itu. VPN yang dinyalakan belakangan tidak diperiksa sendiri.
- **Pantau dan perbaiki sendiri** — memperbaiki tanpa bertanya setiap kali VPN menyala, dan mengembalikan pengaturan di rumah.

Untuk fitur ini, VPN dianggap menyala jika rute utama lewat adapter terowongan, WireGuard, Amnezia, atau TAP. Pergantian zona waktu tidak bergantung pada hal ini: ia hanya melihat IP publik. Jika adapter VPN tidak mirip salah satunya, zona waktu tetap berganti, tetapi program mungkin tidak memperbaiki DNS sendiri.

Setelah perbaikan, program memulai ulang layanan DNS. Jika internet hilang, mulai ulang komputer. Kadang perubahan baru berlaku setelah komputer dimulai ulang.

Tes kebocoran pihak ketiga mungkin masih menampilkan DNS milik VPN itu sendiri, misalnya Cloudflare di negara keluar. Itu DNS terowongan, bukan router rumah.

## Referensi

### Menu

![Menu](screenshots/menu.png)

Tiga baris abu-abu di atas: lokasi, IP dengan nama zona waktu IANA, zona waktu Windows saat ini. Jika zona waktu saat ini tidak cocok dengan zona waktu IP, baris ketiga menampilkan keduanya. Baris yang panjang dipotong; teks lengkapnya ada di tooltip.

- **Periksa sekarang** — pemeriksaan IP tambahan saat itu juga.
- **Jeda ganti otomatis** — pemeriksaan tetap berjalan, zona waktu tidak berubah. “Lanjut ganti otomatis” mencabut jeda dan langsung menyesuaikan zona waktu.
- **Kembalikan zona rumah** — memasang zona waktu rumah yang diingat dan menyalakan jeda; jika tidak, pemeriksaan IP berikutnya akan langsung mengubahnya.
- **Lepas sematan** — hanya tampil jika IP saat ini punya sematan (lihat “Pengaturan”).
- **Perbaiki kebocoran DNS** — hanya tampil jika kebocoran sudah ditemukan.
- **Jalankan sebagai administrator** — hanya tampil jika tidak ada hak administrator.
- **Salin IP** — menyalin IP publik ke papan klip.
- **Pengaturan**, **Tentang**, **Keluar**.

### Pengaturan

![Pengaturan](screenshots/settings.png)

- **Saat keluar, kembalikan zona rumah** — menyala secara bawaan.
- **Ingat zona saat ini** — langsung menjadikan zona waktu saat ini sebagai zona rumah dan mengingat negara Windows saat ini.
- **Ubah negara menurut IP** — mati. Mengatur negara Windows menurut negara IP publik. Hak administrator tidak diperlukan.
- **Saat dimatikan atau diganti, kembalikan negara Anda** — menyala. Negara Anda kembali saat kotak dihapus, saat jeda, lewat “Kembalikan zona rumah”, dan saat keluar.
- **Mulai saat masuk Windows** — tugas Task Scheduler bernama `IpTzTray` dengan hak administrator, tanpa meminta hak lagi. Menghapus centang akan menghapus tugas itu.
- **Berapa lama menunggu jawaban layanan** — 5, 10, atau 20 detik, bawaannya 5. Ini batas untuk setiap langkah: terpisah untuk pemeriksaan IP dan untuk permintaan zona waktu.
- **Kebocoran DNS** — tiga mode dari bagian “Kebocoran DNS”.
- **Bahasa** — bawaannya bahasa Windows. Jika tidak ada di daftar, bahasa Inggris. “Simpan” langsung menerapkan bahasa, tanpa memulai ulang.

Bahasa antarmuka: English, 中文, हिन्दी, Español, العربية, Français, বাংলা, Português, Русский, Bahasa Indonesia. Nama zona waktu berasal dari Windows, jadi tampil dalam bahasa sistem.

Di bagian bawah jendela ada jalur folder `%AppData%\IpTzTray` dan tombol “Buka”. Di folder itu ada `settings.json` dan log `log.txt`. Log ditulis dalam bahasa Rusia. Saat file membesar, bagian sebelumnya disimpan di `log.txt.old`.

**Sematan** — zona waktu pilihan sendiri untuk IP tertentu. Selama sematan ada, program memasang zona waktu itu untuk IP tersebut dan tidak bertanya ke layanan geolokasi. Versi ini tidak punya jendela untuk menyematkan. Sematan bisa ditulis di `settings.json` saat program tertutup, di bidang `Pins`:

```json
"Pins": { "203.0.113.10": "Eastern Standard Time" }
```

Zona waktu ditulis dengan nama Windows. `tzutil /l` menampilkan daftar nama.

### Baris perintah

- `IpTzTray.exe --check` — menampilkan IP dan zona waktu, tidak mengubah apa pun.
- `IpTzTray.exe --dns` — menampilkan apakah kebocoran DNS terlihat, tidak mengubah apa pun.
- `IpTzTray.exe --dry` — ikon dalam mode pratinjau: zona waktu dan DNS tidak berubah, pengaturan ditulis ke folder sementara.
- `IpTzTray.exe --map Europe/Amsterdam` — menampilkan zona waktu Windows untuk nama IANA.
- `IpTzTray.exe --selftest` — memeriksa tabel zona waktu, pembacaan jawaban, dan penyimpanan pengaturan; tidak mengubah sistem.

## Cara kerja

- **IP publik** ditanyakan sekaligus ke tiga layanan: Cloudflare (`1.1.1.1`), ipinfo.io, dan ip-api.com. Alamat yang disebut paling sedikit dua layanan yang dipakai. Jawaban satu layanan diterima hanya jika layanan lain diam. Jika jawaban berbeda, pemeriksaan diulang. Alamat privat dibuang.
- **Frekuensi**: selama IP sama, kira-kira setiap 10 detik. Setelah jaringan berubah, pemeriksaan pertama 5 detik kemudian, lalu setiap 5 detik selama sekitar 25 detik, lalu kembali setiap 10 detik.
- **Zona waktu** ditanyakan bergiliran ke ip-api.com, ipwho.is, ipinfo.io, dan geojs.io, hanya jika IP berubah. Nama zona waktu IANA diubah ke zona waktu Windows dengan tabel Unicode CLDR dan dipasang dengan `tzutil`.
- **Zona waktu rumah** adalah zona waktu Windows saat pertama kali dijalankan, sebelum penyesuaian pertama. Jika saat itu sudah terpasang zona asing, zona itulah yang diingat. Perbaiki dengan “Ingat zona saat ini”.
- **Negara Anda** adalah negara Windows saat pertama kali dijalankan. Tombol yang sama juga memperbaruinya.
- **Apa yang keluar**: layanan pendeteksi IP melihat IP publik Anda di setiap pemeriksaan. Layanan geolokasi menerima IP hanya saat alamat berubah.

## Batasan

- Proxy yang diatur hanya di browser tidak terdeteksi: program melihat IP seluruh sistem.
- Kota pusat data VPN di basis data geolokasi kadang tidak cocok dengan zona waktu yang tepat. Yang dipasang adalah zona waktu yang dikembalikan layanan.
- Tanpa hak administrator, zona waktu dan DNS tidak berubah. Negara Windows tetap bisa berubah.
- Wilayah akun Microsoft tidak berubah. IP yang disematkan tidak punya kode negara, jadi program tidak mengubah negara untuk IP itu.

## Build

C#, Windows Forms, .NET Framework bawaan Windows, x64. Hanya perlu kompiler `csc`, yang sudah ada di Windows. Visual Studio tidak diperlukan.

```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Hasil: `publish\IpTzTray.exe` dan ikon `publish\app.ico`.

- `src` — kode sumber.
- `data/windowsZones.xml` — tabel zona waktu Unicode CLDR.
- `build.ps1` — skrip build.
- `app.manifest` — manifes Windows.

Program ditulis dan di-build dengan Grok 4.7.

## Lisensi dan nama

Gratis untuk penggunaan apa pun, termasuk komersial.

Fork dan build yang diubah punya dua syarat: jangan menghapus nama pembuat dan jangan memakai nama IPTzTray. Teks lengkap ada di [LICENSE](LICENSE).

Pembuat: hello@kubsite.ru  
Halaman program: https://github.com/kubsite1/iptztray

Terima kasih atas dukungannya — agregator pesan [wamm.chat](https://wamm.chat).
