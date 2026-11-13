using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace IpTzTray
{
    static class Lang
    {
        // Порядок строки: en, zh, hi, es, ar, fr, bn, pt, ru, id.
        static readonly string[] Order = { "en", "zh", "hi", "es", "ar", "fr", "bn", "pt", "ru", "id" };
        public static readonly string[] ChoiceCodes = { "", "en", "zh", "hi", "es", "ar", "fr", "bn", "pt", "ru", "id" };
        static readonly Dictionary<string, string[]> Rows = new Dictionary<string, string[]>(StringComparer.Ordinal);
        static int _index;
        static Font _font;
        static bool _ownFont;
        public static string Code = "en";

        static Lang()
        {
            Add("menu.check",
                "Check now", "立即检查", "अभी जाँचें", "Comprobar ahora", "تحقق الآن", "Vérifier maintenant", "এখনই পরীক্ষা", "Verificar agora", "Проверить сейчас", "Periksa sekarang");
            Add("menu.pause",
                "Pause auto-switch", "暂停自动切换", "स्वतः बदलाव रोकें", "Pausar el cambio automático", "إيقاف التبديل التلقائي", "Pause du changement auto", "স্বয়ংক্রিয় বদল থামান", "Pausar a troca automática", "Пауза автосмены", "Jeda ganti otomatis");
            Add("menu.resume",
                "Resume auto-switch", "继续自动切换", "स्वतः बदलाव जारी रखें", "Reanudar el cambio automático", "متابعة التبديل التلقائي", "Reprendre le changement auto", "স্বয়ংক্রিয় বদল চালু", "Continuar a troca automática", "Продолжить автосмену", "Lanjut ganti otomatis");
            Add("menu.home",
                "Restore home time zone", "恢复家庭时区", "घरेलू समय क्षेत्र लौटाएँ", "Volver al huso de casa", "إرجاع المنطقة المنزلية", "Revenir au fuseau domicile", "বাড়ির সময় অঞ্চল ফেরান", "Voltar ao fuso de casa", "Вернуть домашний пояс", "Kembalikan zona rumah");
            Add("menu.unpin",
                "Remove pin", "取消固定", "पिन हटाएँ", "Quitar la fijación", "إلغاء التثبيت", "Retirer l’épingle", "পিন সরান", "Remover a fixação", "Снять закрепление", "Lepas sematan");
            Add("menu.elevate",
                "Run as administrator", "以管理员身份运行", "व्यवस्थापक के रूप में चलाएँ", "Ejecutar como administrador", "التشغيل كمسؤول", "Exécuter en administrateur", "প্রশাসক হিসেবে চালান", "Executar como administrador", "Запустить с правами администратора", "Jalankan sebagai administrator");
            Add("menu.copy",
                "Copy IP", "复制 IP", "IP कॉपी करें", "Copiar IP", "نسخ IP", "Copier l’IP", "IP কপি", "Copiar IP", "Копировать IP", "Salin IP");
            Add("menu.dns",
                "Fix DNS leak…", "修复 DNS 泄漏…", "DNS रिसाव ठीक करें…", "Corregir fuga de DNS…", "إصلاح تسرب DNS…", "Corriger la fuite DNS…", "DNS ফাঁস ঠিক করুন…", "Corrigir vazamento de DNS…", "Исправить утечку DNS…", "Perbaiki kebocoran DNS…");
            Add("menu.settings",
                "Settings…", "设置…", "सेटिंग…", "Ajustes…", "الإعدادات…", "Paramètres…", "সেটিংস…", "Configurações…", "Настройки…", "Pengaturan…");
            Add("menu.about",
                "About", "关于", "परिचय", "Acerca de", "حول البرنامج", "À propos", "পরিচিতি", "Sobre", "О программе", "Tentang");
            Add("menu.exit",
                "Exit", "退出", "बाहर जाएँ", "Salir", "خروج", "Quitter", "প্রস্থান", "Sair", "Выход", "Keluar");

            Add("status.checking",
                "Checking the public IP…", "正在检查公网 IP…", "सार्वजनिक IP जाँचा जा रहा है…", "Comprobando la IP pública…", "جارٍ التحقق من IP العام…", "Vérification de l’IP publique…", "সর্বজনীন IP পরীক্ষা হচ্ছে…", "Verificando o IP público…", "Проверяю внешний IP…", "Memeriksa IP publik…");
            Add("status.noip",
                "IP not known yet", "尚未知道 IP", "IP अभी पता नहीं", "La IP aún no se conoce", "IP غير معروف بعد", "IP encore inconnue", "IP এখনও জানা নেই", "IP ainda desconhecido", "IP ещё не известен", "IP belum diketahui");
            Add("status.pinned",
                "pinned", "已固定", "पिन किया", "fijado", "مثبّت", "épinglé", "পিন করা", "fixado", "закреплено", "disematkan");
            Add("status.now",
                "Now {0}. By IP: {1}", "当前 {0}。按 IP：{1}", "अभी {0}। IP के अनुसार: {1}", "Ahora {0}. Según la IP: {1}", "الآن {0}. حسب IP: {1}", "Maintenant {0}. Selon l’IP : {1}", "এখন {0}। IP অনুসারে: {1}", "Agora {0}. Pelo IP: {1}", "Сейчас {0}. По IP: {1}", "Sekarang {0}. Menurut IP: {1}");
            Add("tip.pause",
                "paused · {0}", "已暂停 · {0}", "रुका है · {0}", "en pausa · {0}", "متوقف · {0}", "en pause · {0}", "থামা · {0}", "em pausa · {0}", "пауза · {0}", "dijeda · {0}");
            Add("tip.mismatch",
                " · does not match", " · 不一致", " · मेल नहीं", " · no coincide", " · غير مطابق", " · ne correspond pas", " · মেলে না", " · não confere", " · не совпадает", " · tidak cocok");

            Add("balloon.paused",
                "Auto-switch is paused. By IP: {0}", "自动切换已暂停。按 IP：{0}", "स्वतः बदलाव रुका है। IP के अनुसार: {0}", "El cambio automático está en pausa. Según la IP: {0}", "التبديل التلقائي متوقف. حسب IP: {0}", "Le changement auto est en pause. Selon l’IP : {0}", "স্বয়ংক্রিয় বদল থেমে আছে। IP অনুসারে: {0}", "A troca automática está em pausa. Pelo IP: {0}", "Автосмена пояса на паузе. По IP: {0}", "Ganti otomatis dijeda. Menurut IP: {0}");
            Add("balloon.dry",
                "Preview, time zone unchanged. By IP: {0}", "预览，不更改时区。按 IP：{0}", "पूर्वावलोकन, समय क्षेत्र नहीं बदला। IP के अनुसार: {0}", "Vista previa, el huso no cambia. Según la IP: {0}", "معاينة، المنطقة لم تتغير. حسب IP: {0}", "Aperçu, fuseau inchangé. Selon l’IP : {0}", "প্রিভিউ, সময় অঞ্চল বদলায়নি। IP অনুসারে: {0}", "Prévia, fuso não alterado. Pelo IP: {0}", "Просмотр, пояс не меняю. По IP: {0}", "Pratinjau, zona tidak diubah. Menurut IP: {0}");
            Add("balloon.noadmin.ip",
                "No administrator rights. By IP: {0}", "没有管理员权限。按 IP：{0}", "व्यवस्थापक अधिकार नहीं हैं। IP के अनुसार: {0}", "Sin permisos de administrador. Según la IP: {0}", "لا توجد صلاحية مسؤول. حسب IP: {0}", "Pas de droits d’administrateur. Selon l’IP : {0}", "প্রশাসকের অধিকার নেই। IP অনুসারে: {0}", "Sem direitos de administrador. Pelo IP: {0}", "Нет прав администратора. По IP: {0}", "Tidak ada hak administrator. Menurut IP: {0}");
            Add("balloon.same",
                "Time zone matches the IP: {0}", "时区与 IP 一致：{0}", "समय क्षेत्र IP से मेल खाता है: {0}", "El huso coincide con la IP: {0}", "المنطقة تطابق IP: {0}", "Le fuseau correspond à l’IP : {0}", "সময় অঞ্চল IP-এর সাথে মেলে: {0}", "O fuso confere com o IP: {0}", "Пояс совпадает с IP: {0}", "Zona cocok dengan IP: {0}");
            Add("balloon.fail",
                "Could not change the time zone", "未能更改时区", "समय क्षेत्र बदला नहीं जा सका", "No se pudo cambiar el huso", "تعذر تغيير المنطقة", "Impossible de changer le fuseau", "সময় অঞ্চল বদলানো যায়নি", "Não foi possível mudar o fuso", "Не удалось сменить пояс", "Tidak dapat mengubah zona");
            Add("balloon.set",
                "Time zone set: {0}", "已设置时区：{0}", "समय क्षेत्र सेट हुआ: {0}", "Huso establecido: {0}", "تم ضبط المنطقة: {0}", "Fuseau défini : {0}", "সময় অঞ্চল সেট হয়েছে: {0}", "Fuso definido: {0}", "Поставил пояс: {0}", "Zona diatur: {0}");
            Add("welcome.home",
                "Home time zone remembered: {0}.", "已记住家庭时区：{0}。", "घरेलू समय क्षेत्र याद रखा: {0}.", "Huso de casa recordado: {0}.", "تم تذكر المنطقة المنزلية: {0}.", "Fuseau domicile mémorisé : {0}.", "বাড়ির সময় অঞ্চল মনে রাখা হয়েছে: {0}।", "Fuso de casa memorizado: {0}.", "Домашний запомнен: {0}.", "Zona rumah diingat: {0}.");
            Add("welcome.auto",
                "It starts at sign-in.", "登录时会自行启动。", "साइन-इन पर यह अपने आप चलेगा।", "Se inicia al entrar en Windows.", "يبدأ عند تسجيل الدخول.", "Il démarre à l’ouverture de session.", "সাইন-ইন করলে এটি নিজে চালু হয়।", "Ele inicia ao entrar no Windows.", "При входе утилита запускается сама.", "Aplikasi mulai sendiri saat masuk Windows.");
            Add("balloon.dry.welcome",
                "Preview, time zone unchanged. By IP now {0}. {1}", "预览，不更改时区。按 IP 现在是 {0}。{1}", "पूर्वावलोकन, समय क्षेत्र नहीं बदला। IP के अनुसार अभी {0}। {1}", "Vista previa, el huso no cambia. Según la IP ahora {0}. {1}", "معاينة، المنطقة لم تتغير. حسب IP الآن {0}. {1}", "Aperçu, fuseau inchangé. Selon l’IP maintenant {0}. {1}", "প্রিভিউ, সময় অঞ্চল বদলায়নি। IP অনুসারে এখন {0}। {1}", "Prévia, fuso não alterado. Pelo IP agora {0}. {1}", "Просмотр, пояс не меняю. По IP сейчас {0}. {1}", "Pratinjau, zona tidak diubah. Menurut IP sekarang {0}. {1}");
            Add("balloon.set.welcome",
                "Set the time zone from the IP: {0}. {1}", "已按 IP 设置时区：{0}。{1}", "IP के अनुसार समय क्षेत्र सेट हुआ: {0}। {1}", "Huso puesto según la IP: {0}. {1}", "تم ضبط المنطقة حسب IP: {0}. {1}", "Fuseau mis selon l’IP : {0}. {1}", "IP অনুসারে সময় অঞ্চল সেট: {0}। {1}", "Fuso definido pelo IP: {0}. {1}", "Поставил пояс по IP: {0}. {1}", "Zona diatur dari IP: {0}. {1}");
            Add("balloon.same.welcome",
                "The time zone already matches the public IP. {0}", "时区已经与公网 IP 一致。{0}", "समय क्षेत्र पहले से सार्वजनिक IP से मेल खाता है। {0}", "El huso ya coincide con la IP pública. {0}", "المنطقة تطابق IP العام مسبقاً. {0}", "Le fuseau correspond déjà à l’IP publique. {0}", "সময় অঞ্চল আগেই সর্বজনীন IP-এর সাথে মেলে। {0}", "O fuso já confere com o IP público. {0}", "Пояс уже совпадает с внешним IP. {0}", "Zona sudah cocok dengan IP publik. {0}");
            Add("balloon.dry.place",
                "Preview: the IP needs time zone {0}. The system was not changed.", "预览：此 IP 需要时区 {0}。系统未更改。", "पूर्वावलोकन: इस IP के लिए समय क्षेत्र {0} चाहिए। सिस्टम नहीं बदला।", "Vista previa: la IP necesita el huso {0}. El sistema no cambió.", "معاينة: يحتاج IP إلى المنطقة {0}. لم يتغير النظام.", "Aperçu : l’IP demande le fuseau {0}. Le système n’a pas changé.", "প্রিভিউ: এই IP-এর জন্য সময় অঞ্চল {0} দরকার। সিস্টেম বদলায়নি।", "Prévia: o IP precisa do fuso {0}. O sistema não mudou.", "Просмотр: по IP нужен пояс {0}. Система не изменена.", "Pratinjau: IP membutuhkan zona {0}. Sistem tidak diubah.");
            Add("balloon.place",
                "Time zone: {0}", "时区：{0}", "समय क्षेत्र: {0}", "Huso: {0}", "المنطقة: {0}", "Fuseau : {0}", "সময় অঞ্চল: {0}", "Fuso: {0}", "Часовой пояс: {0}", "Zona waktu: {0}");
            Add("balloon.home.pause",
                "Home time zone is on. Auto-switch is paused, or the next IP would change it again. {0} is in the menu.", "已启用家庭时区。自动切换已暂停，否则下一个 IP 会再次改掉它。菜单里有{0}。", "घरेलू समय क्षेत्र लग गया है। स्वतः बदलाव रुका है, नहीं तो अगला IP इसे फिर बदल देगा। मेनू में {0} है।", "El huso de casa está activo. El cambio automático está en pausa; si no, la siguiente IP lo cambiaría. {0} está en el menú.", "المنطقة المنزلية مفعّلة. التبديل التلقائي متوقف، وإلا غيّرها IP التالي. {0} في القائمة.", "Le fuseau domicile est actif. Le changement auto est en pause, sinon la prochaine IP le changerait. {0} est dans le menu.", "বাড়ির সময় অঞ্চল চালু। স্বয়ংক্রিয় বদল থেমে আছে, না হলে পরের IP আবার বদলে দেবে। মেনুতে {0} আছে।", "O fuso de casa está ativo. A troca automática está em pausa; senão o próximo IP mudaria de novo. {0} está no menu.", "Домашний пояс включён. Автосмена на паузе, иначе следующий IP снова его перебьёт. «{0}» — в меню.", "Zona rumah aktif. Ganti otomatis dijeda, kalau tidak IP berikutnya akan mengubahnya lagi. {0} ada di menu.");
            Add("balloon.home",
                "Home time zone: {0}", "家庭时区：{0}", "घरेलू समय क्षेत्र: {0}", "Huso de casa: {0}", "المنطقة المنزلية: {0}", "Fuseau domicile : {0}", "বাড়ির সময় অঞ্চল: {0}", "Fuso de casa: {0}", "Домашний пояс: {0}", "Zona rumah: {0}");
            Add("balloon.copy",
                "IP {0} copied to the clipboard", "IP {0} 已复制到剪贴板", "IP {0} क्लिपबोर्ड पर कॉपी हुआ", "IP {0} copiada al portapapeles", "تم نسخ IP {0} إلى الحافظة", "IP {0} copiée dans le presse-papiers", "IP {0} ক্লিপবোর্ডে কপি হয়েছে", "IP {0} copiado para a área de transferência", "IP {0} скопирован в буфер", "IP {0} disalin ke papan klip");
            Add("balloon.copyfail",
                "Could not copy the IP: {0}", "未能复制 IP：{0}", "IP कॉपी नहीं हुआ: {0}", "No se pudo copiar la IP: {0}", "تعذر نسخ IP: {0}", "Impossible de copier l’IP : {0}", "IP কপি হয়নি: {0}", "Não foi possível copiar o IP: {0}", "Не удалось скопировать IP: {0}", "Tidak dapat menyalin IP: {0}");
            Add("balloon.other",
                "Another copy is already running.", "另一个副本已在运行。", "एक और प्रति पहले से चल रही है।", "Ya hay otra copia en ejecución.", "هناك نسخة أخرى تعمل بالفعل.", "Une autre copie est déjà lancée.", "আরেকটি কপি আগে থেকেই চলছে।", "Outra cópia já está em execução.", "Уже запущена другая копия.", "Salinan lain sudah berjalan.");
            Add("balloon.noadmin",
                "Without administrator rights the Windows time zone does not change.", "没有管理员权限时，Windows 时区不会改变。", "व्यवस्थापक अधिकार के बिना Windows का समय क्षेत्र नहीं बदलता।", "Sin permisos de administrador el huso de Windows no cambia.", "بلا صلاحية مسؤول لا تتغير منطقة Windows.", "Sans droits d’administrateur le fuseau Windows ne change pas.", "প্রশাসকের অধিকার না থাকলে Windows-এর সময় অঞ্চল বদলায় না।", "Sem direitos de administrador o fuso do Windows não muda.", "Без прав администратора пояс Windows не меняется.", "Tanpa hak administrator zona Windows tidak berubah.");
            Add("balloon.dns.verifyfail",
                "Check after the fix failed: {0}", "修复后的检查失败：{0}", "सुधार के बाद जाँच विफल: {0}", "La comprobación tras corregir falló: {0}", "فشل التحقق بعد الإصلاح: {0}", "La vérification après correction a échoué : {0}", "ঠিক করার পর পরীক্ষা ব্যর্থ: {0}", "A verificação depois da correção falhou: {0}", "Проверка после исправления не удалась: {0}", "Pemeriksaan setelah perbaikan gagal: {0}");
            Add("balloon.dns.fixed",
                "Done: the extra DNS is off. On the home network I will put it back.", "完成：多余的 DNS 已关闭。在家庭网络上我会改回去。", "हो गया: अतिरिक्त DNS बंद है। घरेलू नेटवर्क पर इसे वापस कर दूँगा।", "Listo: el DNS extra está apagado. En la red de casa lo devolveré.", "تم: أُوقف DNS الزائد. على الشبكة المنزلية سأعيده.", "Fait : le DNS en trop est coupé. Sur le réseau de la maison je le remettrai.", "হয়ে গেছে: অতিরিক্ত DNS বন্ধ। বাড়ির নেটওয়ার্কে আমি ফিরিয়ে দেব।", "Pronto: o DNS extra está desligado. Na rede de casa eu devolvo.", "Готово: лишний DNS выключен. На домашней сети верну как было.", "Selesai: DNS ekstra dimatikan. Di jaringan rumah saya kembalikan seperti semula.");
            Add("balloon.dns.still",
                "Checked again: the leak is still there. {0}", "又检查了一次：泄漏还在。{0}", "फिर जाँचा: रिसाव अभी भी है। {0}", "Comprobado otra vez: la fuga sigue. {0}", "أُعيد التحقق: التسرب ما زال موجوداً. {0}", "Vérifié encore : la fuite est toujours là. {0}", "আবার পরীক্ষা: ফাঁস এখনও আছে। {0}", "Verifiquei de novo: o vazamento continua. {0}", "Проверил ещё раз: утечка осталась. {0}", "Diperiksa lagi: kebocoran masih ada. {0}");
            Add("balloon.dns.norestore",
                "Home network. No rights to restore the previous DNS.", "家庭网络。没有权限恢复原来的 DNS。", "घरेलू नेटवर्क। पिछला DNS वापस करने का अधिकार नहीं है।", "Red de casa. No hay permisos para devolver el DNS anterior.", "شبكة منزلية. لا صلاحية لإرجاع DNS السابق.", "Réseau de la maison. Pas de droits pour remettre l’ancien DNS.", "বাড়ির নেটওয়ার্ক। আগের DNS ফেরানোর অধিকার নেই।", "Rede de casa. Sem direitos para restaurar o DNS anterior.", "Домашняя сеть. Нет прав вернуть прежний DNS.", "Jaringan rumah. Tidak ada hak untuk mengembalikan DNS sebelumnya.");

            Add("err.noadmin",
                "No administrator rights", "没有管理员权限", "व्यवस्थापक अधिकार नहीं", "Sin permisos de administrador", "لا توجد صلاحية مسؤول", "Pas de droits d’administrateur", "প্রশাসকের অধিকার নেই", "Sem direitos de administrador", "Нет прав администратора", "Tidak ada hak administrator");
            Add("err.autostart",
                "Autostart was not enabled: {0}", "未能启用开机启动：{0}", "ऑटोस्टार्ट चालू नहीं हुआ: {0}", "No se activó el inicio automático: {0}", "لم يُفعّل التشغيل التلقائي: {0}", "Le démarrage automatique n’a pas été activé : {0}", "অটোস্টার্ট চালু হয়নি: {0}", "A inicialização automática não foi ativada: {0}", "Автозапуск не включился: {0}", "Mulai otomatis tidak aktif: {0}");
            Add("err.ip.split",
                "The services reported different IPs, I will retry", "几个服务给出的 IP 不同，我将重试", "सेवाओं ने अलग-अलग IP बताए, फिर कोशिश करूँगा", "Los servicios dieron IP distintas, reintentaré", "أبلغت الخدمات عن IP مختلفة، سأعيد المحاولة", "Les services ont donné des IP différentes, je réessaie", "সেবাগুলো আলাদা IP দিয়েছে, আবার চেষ্টা করব", "Os serviços informaram IPs diferentes, vou tentar de novo", "Сервисы показали разные IP, повторю", "Layanan memberi IP yang berbeda, saya coba lagi");
            Add("err.ip.none",
                "Could not learn the public IP", "未能获知公网 IP", "सार्वजनिक IP पता नहीं चल सका", "No se pudo conocer la IP pública", "تعذر معرفة IP العام", "Impossible de connaître l’IP publique", "সর্বজনীন IP জানা যায়নি", "Não foi possível saber o IP público", "Не удалось узнать внешний IP", "Tidak dapat mengetahui IP publik");
            Add("err.geo",
                "Could not determine the time zone ({0})", "未能确定时区（{0}）", "समय क्षेत्र पता नहीं चल सका ({0})", "No se pudo determinar el huso ({0})", "تعذر تحديد المنطقة ({0})", "Impossible de déterminer le fuseau ({0})", "সময় অঞ্চল নির্ধারণ করা যায়নি ({0})", "Não foi possível determinar o fuso ({0})", "Не удалось определить пояс ({0})", "Tidak dapat menentukan zona ({0})");
            Add("err.geo.empty",
                "no answer", "无响应", "कोई उत्तर नहीं", "sin respuesta", "لا جواب", "pas de réponse", "কোনো উত্তর নেই", "sem resposta", "нет ответа", "tidak ada jawaban");
            Add("err.geo.timeout",
                "timed out", "已超时", "समय समाप्त", "se agotó el tiempo", "انتهت المهلة", "délai dépassé", "সময় শেষ", "tempo esgotado", "время ожидания вышло", "waktu habis");
            Add("err.geo.nozone",
                "the answer has no time zone", "回复里没有时区", "उत्तर में समय क्षेत्र नहीं है", "la respuesta no trae huso", "الرد بلا منطقة زمنية", "la réponse n’a pas de fuseau", "উত্তরে সময় অঞ্চল নেই", "a resposta não traz fuso", "ответ без часового пояса", "jawaban tanpa zona waktu");
            Add("err.geo.nowin",
                "no Windows time zone for {0}", "没有适用于 {0} 的 Windows 时区", "{0} के लिए Windows समय क्षेत्र नहीं", "no hay huso de Windows para {0}", "لا توجد منطقة Windows لـ {0}", "pas de fuseau Windows pour {0}", "{0}-এর জন্য Windows সময় অঞ্চল নেই", "não há fuso do Windows para {0}", "нет пояса Windows для {0}", "tidak ada zona Windows untuk {0}");
            Add("err.net",
                "network error", "网络错误", "नेटवर्क त्रुटि", "error de red", "خطأ في الشبكة", "erreur réseau", "নেটওয়ার্ক ত্রুটি", "erro de rede", "ошибка сети", "kesalahan jaringan");
            Add("err.long",
                "the answer is too long", "回复过长", "उत्तर बहुत लंबा है", "la respuesta es demasiado larga", "الرد طويل جداً", "la réponse est trop longue", "উত্তর অনেক লম্বা", "a resposta é longa demais", "слишком длинный ответ", "jawaban terlalu panjang");
            Add("err.nostart",
                "Could not start {0}", "未能启动 {0}", "{0} शुरू नहीं हुआ", "No se pudo iniciar {0}", "تعذر تشغيل {0}", "Impossible de lancer {0}", "{0} চালু হয়নি", "Não foi possível iniciar {0}", "Не запустился {0}", "Tidak dapat menjalankan {0}");
            Add("err.exit",
                "{0} code {1} {2}", "{0} 代码 {1} {2}", "{0} कोड {1} {2}", "{0} código {1} {2}", "{0} رمز {1} {2}", "{0} code {1} {2}", "{0} কোড {1} {2}", "{0} código {1} {2}", "{0} код {1} {2}", "{0} kode {1} {2}");

            Add("about.title",
                "About", "关于", "परिचय", "Acerca de", "حول البرنامج", "À propos", "পরিচিতি", "Sobre", "О программе", "Tentang");
            Add("about.version",
                "Version {0}", "版本 {0}", "संस्करण {0}", "Versión {0}", "الإصدار {0}", "Version {0}", "সংস্করণ {0}", "Versão {0}", "Версия {0}", "Versi {0}");
            Add("about.author",
                "Author: {0}", "作者：{0}", "लेखक: {0}", "Autor: {0}", "المؤلف: {0}", "Auteur : {0}", "লেখক: {0}", "Autor: {0}", "Автор: {0}", "Pembuat: {0}");

            Add("set.title",
                "Settings", "设置", "सेटिंग", "Ajustes", "الإعدادات", "Paramètres", "সেটিংস", "Configurações", "Настройки", "Pengaturan");
            Add("set.restore",
                "Restore the home time zone on exit", "退出时恢复家庭时区", "बाहर निकलने पर घरेलू समय क्षेत्र लौटाएँ", "Al salir, volver al huso de casa", "عند الخروج أرجع المنطقة المنزلية", "À la fermeture, revenir au fuseau domicile", "বন্ধ করলে বাড়ির সময় অঞ্চল ফেরান", "Ao sair, voltar ao fuso de casa", "При выходе возвращать домашний пояс", "Saat keluar, kembalikan zona rumah");
            Add("set.home.none",
                "Home time zone is not remembered yet.", "尚未记住家庭时区。", "घरेलू समय क्षेत्र अभी याद नहीं है।", "El huso de casa aún no está recordado.", "المنطقة المنزلية لم تُحفظ بعد.", "Le fuseau domicile n’est pas encore mémorisé.", "বাড়ির সময় অঞ্চল এখনও মনে নেই।", "O fuso de casa ainda não foi memorizado.", "Домашний пояс ещё не запомнен.", "Zona rumah belum diingat.");
            Add("set.home",
                "Home: {0}", "家庭：{0}", "घर: {0}", "Casa: {0}", "المنزل: {0}", "Domicile : {0}", "বাড়ি: {0}", "Casa: {0}", "Домашний: {0}", "Rumah: {0}");
            Add("set.remember",
                "Remember the current time zone", "记住当前时区", "वर्तमान समय क्षेत्र याद रखें", "Recordar el huso actual", "تذكر المنطقة الحالية", "Mémoriser le fuseau actuel", "বর্তমান সময় অঞ্চল মনে রাখুন", "Memorizar o fuso atual", "Запомнить текущий пояс", "Ingat zona saat ini");
            Add("set.autostart",
                "Start at Windows sign-in", "登录 Windows 时启动", "Windows साइन-इन पर चलाएँ", "Iniciar al entrar en Windows", "ابدأ عند تسجيل الدخول إلى Windows", "Démarrer à l’ouverture de session", "Windows সাইন-ইনে চালু করুন", "Iniciar ao entrar no Windows", "Запускать при входе в Windows", "Mulai saat masuk Windows");
            Add("set.wait",
                "How long to wait for the service", "等待服务回复多久", "सेवा के उत्तर की प्रतीक्षा कितनी देर", "Cuánto esperar la respuesta del servicio", "كم ننتظر رد الخدمة", "Combien attendre la réponse du service", "সেবার উত্তরের জন্য কতক্ষণ অপেক্ষা", "Quanto esperar a resposta do serviço", "Сколько ждать ответ сервиса", "Berapa lama menunggu jawaban layanan");
            Add("set.sec5",
                "5 seconds", "5 秒", "5 सेकंड", "5 segundos", "5 ثوان", "5 secondes", "৫ সেকেন্ড", "5 segundos", "5 секунд", "5 detik");
            Add("set.sec10",
                "10 seconds", "10 秒", "10 सेकंड", "10 segundos", "10 ثوان", "10 secondes", "১০ সেকেন্ড", "10 segundos", "10 секунд", "10 detik");
            Add("set.sec20",
                "20 seconds", "20 秒", "20 सेकंड", "20 segundos", "20 ثانية", "20 secondes", "২০ সেকেন্ড", "20 segundos", "20 секунд", "20 detik");
            Add("set.poll",
                "While the IP stays the same, retry about every 10 seconds.", "IP 不变时，大约每 10 秒再查一次。", "जब तक IP वही रहे, लगभग हर 10 सेकंड में फिर देखें।", "Mientras la IP no cambie, se repite unos cada 10 segundos.", "ما دام IP كما هو، تُعاد المحاولة كل 10 ثوان تقريباً.", "Tant que l’IP ne change pas, nouvel essai environ toutes les 10 secondes.", "IP একই থাকলে প্রায় প্রতি ১০ সেকেন্ডে আবার দেখা হয়।", "Enquanto o IP for o mesmo, nova tentativa a cada cerca de 10 segundos.", "Пока IP тот же, повтор около 10 секунд.", "Selama IP sama, coba lagi kira-kira setiap 10 detik.");
            Add("set.dns",
                "DNS leak", "DNS 泄漏", "DNS रिसाव", "Fuga de DNS", "تسرب DNS", "Fuite DNS", "DNS ফাঁস", "Vazamento de DNS", "Утечка DNS", "Kebocoran DNS");
            Add("set.dns.off",
                "Do not check", "不检查", "जाँच न करें", "No comprobar", "لا تتحقق", "Ne pas vérifier", "পরীক্ষা করবেন না", "Não verificar", "Не проверять", "Jangan periksa");
            Add("set.dns.once",
                "Once at startup", "启动时一次", "शुरू में एक बार", "Una vez al iniciar", "مرة عند التشغيل", "Une fois au démarrage", "চালু হওয়ার সময় একবার", "Uma vez ao iniciar", "Один раз при запуске", "Sekali saat mulai");
            Add("set.dns.watch",
                "Watch and fix by itself", "自行监视并修复", "खुद देखें और ठीक करें", "Vigilar y corregir solo", "راقب وأصلح من نفسه", "Surveiller et corriger tout seul", "নিজে দেখুন এবং ঠিক করুন", "Vigiar e corrigir sozinho", "Следить и исправлять само", "Pantau dan perbaiki sendiri");
            Add("set.dns.oncehint",
                "If the VPN starts later, it will not check on its own.", "如果 VPN 稍后才打开，它不会自己再查。", "अगर VPN बाद में चले, तो यह खुद नहीं जाँचेगा।", "Si la VPN se enciende después, no comprobará sola.", "إذا اشتغل VPN لاحقاً فلن يتحقق من نفسه.", "Si le VPN s’allume plus tard, il ne vérifiera pas tout seul.", "VPN পরে চালু হলে এটি নিজে পরীক্ষা করবে না।", "Se a VPN ligar depois, ele não verifica sozinho.", "Если VPN включится позже, сама не проверит.", "Jika VPN menyala nanti, aplikasi tidak memeriksa sendiri.");
            Add("set.dns.watchhint",
                "On VPN it fixes itself; at home it restores the previous setting.", "在 VPN 上它自己修复，在家则恢复原来的设置。", "VPN पर यह खुद ठीक करता है, घर पर पिछली सेटिंग लौटाता है।", "En la VPN lo corrige solo; en casa devuelve el ajuste anterior.", "على VPN يصلح من نفسه، وفي البيت يعيد الإعداد السابق.", "Sur le VPN il corrige tout seul ; à la maison il remet l’ancien réglage.", "VPN-এ এটি নিজে ঠিক করে, বাড়িতে আগের সেটিং ফেরায়।", "Na VPN ele corrige sozinho; em casa devolve o ajuste anterior.", "На VPN исправляет само, дома возвращает как было.", "Di VPN ia memperbaiki sendiri; di rumah mengembalikan pengaturan semula.");
            Add("set.files",
                "Settings and log", "设置和日志", "सेटिंग और पत्रिका", "Ajustes y registro", "الإعدادات والسجل", "Paramètres et journal", "সেটিংস ও লগ", "Configurações e registro", "Настройки и журнал", "Pengaturan dan log");
            Add("set.open",
                "Open", "打开", "खोलें", "Abrir", "فتح", "Ouvrir", "খুলুন", "Abrir", "Открыть", "Buka");
            Add("set.save",
                "Save", "保存", "सहेजें", "Guardar", "حفظ", "Enregistrer", "সংরক্ষণ", "Salvar", "Сохранить", "Simpan");
            Add("set.lang",
                "Language", "语言", "भाषा", "Idioma", "اللغة", "Langue", "ভাষা", "Idioma", "Язык", "Bahasa");

            Add("dns.title",
                "DNS leak", "DNS 泄漏", "DNS रिसाव", "Fuga de DNS", "تسرب DNS", "Fuite DNS", "DNS ফাঁস", "Vazamento de DNS", "Утечка DNS", "Kebocoran DNS");
            Add("dns.router",
                "the router", "路由器", "राउटर", "el enrutador", "الموجه", "le routeur", "রাউটার", "o roteador", "роутера", "router");
            Add("dns.later",
                "Not now", "以后再说", "अभी नहीं", "Ahora no", "ليس الآن", "Pas maintenant", "এখন নয়", "Agora não", "Не сейчас", "Nanti saja");
            Add("dns.fix",
                "Fix", "修复", "ठीक करें", "Corregir", "إصلاح", "Corriger", "ঠিক করুন", "Corrigir", "Исправить", "Perbaiki");
            Add("dns.body",
                "Besides the VPN, Windows also asks the DNS of «{0}». The query leaves the tunnel, so the provider can see site names.\r\n\r\nFix stops that extra lookup while the VPN is on. On the home network the setting returns by itself. Close and Not now change nothing.",
                "除了 VPN，Windows 还会查询「{0}」的 DNS。请求会绕过隧道，运营商能看到网站名称。\r\n\r\n“修复”会在 VPN 开启时关掉这次多余查询。回到家庭网络后设置会自行恢复。关闭窗口和“以后再说”不会改任何东西。",
                "VPN के अलावा Windows «{0}» का DNS भी पूछता है। अनुरोध टनल के बाहर जाता है, इसलिए प्रदाता साइटों के नाम देख सकता है।\r\n\r\n“ठीक करें” VPN चालू रहने तक यह अतिरिक्त पूछताछ बंद कर देता है। घर के नेटवर्क पर सेटिंग अपने आप लौट आती है। बंद करना और “अभी नहीं” कुछ नहीं बदलते।",
                "Además de la VPN, Windows consulta el DNS de «{0}». La petición sale del túnel y el proveedor ve los nombres de los sitios.\r\n\r\nCorregir desactiva esa consulta extra mientras la VPN está activa. En la red de casa el ajuste vuelve solo. Cerrar y Ahora no no cambian nada.",
                "إلى جانب VPN، يسأل Windows أيضاً DNS الخاص بـ«{0}». يخرج الطلب من النفق، فيرى المزود أسماء المواقع.\r\n\r\n«إصلاح» يوقف هذا الاستعلام الزائد ما دام VPN يعمل. على الشبكة المنزلية يعود الإعداد من نفسه. الإغلاق و«ليس الآن» لا يغيران شيئاً.",
                "En plus du VPN, Windows interroge aussi le DNS de «{0}». La requête sort du tunnel et le fournisseur voit les noms des sites.\r\n\r\nCorriger coupe cette requête en trop tant que le VPN est actif. Sur le réseau de la maison le réglage revient tout seul. Fermer et Pas maintenant ne changent rien.",
                "VPN ছাড়াও Windows «{0}»-এর DNS জিজ্ঞাসা করে। অনুরোধ টানেলের বাইরে যায়, তাই সরবরাহকারী সাইটের নাম দেখতে পায়।\r\n\r\n“ঠিক করুন” VPN চালু থাকা পর্যন্ত এই অতিরিক্ত জিজ্ঞাসা বন্ধ করে। বাড়ির নেটওয়ার্কে সেটিং নিজে ফিরে আসে। বন্ধ করা এবং “এখন নয়” কিছু বদলায় না।",
                "Além da VPN, o Windows também consulta o DNS de «{0}». O pedido sai do túnel e o provedor vê os nomes dos sites.\r\n\r\nCorrigir desliga essa consulta extra enquanto a VPN está ligada. Na rede de casa o ajuste volta sozinho. Fechar e Agora não não mudam nada.",
                "Windows кроме VPN спрашивает DNS «{0}». Запрос уходит мимо туннеля, провайдер видит имена сайтов.\r\n\r\n«Исправить» запрещает этот лишний опрос, пока включён VPN. На домашней сети настройка сама вернётся. Крестик и «Не сейчас» ничего не меняют.",
                "Selain VPN, Windows juga menanyakan DNS «{0}». Permintaan keluar dari terowongan, sehingga penyedia melihat nama situs.\r\n\r\nPerbaiki mematikan permintaan ekstra itu selama VPN menyala. Di jaringan rumah pengaturan kembali sendiri. Menutup dan Nanti saja tidak mengubah apa pun.");

            Add("dns.closed",
                "DNS is closed, the previous setting is remembered", "DNS 已关闭，原来的设置已记住", "DNS बंद है, पिछली सेटिंग याद है", "DNS cerrado, el ajuste anterior está recordado", "DNS مغلق، والإعداد السابق محفوظ", "DNS fermé, l’ancien réglage est mémorisé", "DNS বন্ধ, আগের সেটিং মনে আছে", "DNS fechado, o ajuste anterior está memorizado", "DNS закрыт, прежняя настройка запомнена", "DNS ditutup, pengaturan sebelumnya diingat");
            Add("dns.noparallel",
                "Windows is not asking every adapter’s DNS", "Windows 没有在查询所有适配器的 DNS", "Windows सभी अडैप्टर का DNS नहीं पूछ रहा", "Windows no consulta el DNS de todos los adaptadores", "Windows لا يسأل DNS كل المحولات", "Windows n’interroge pas le DNS de tous les adaptateurs", "Windows সব অ্যাডাপ্টারের DNS জিজ্ঞাসা করছে না", "O Windows não consulta o DNS de todos os adaptadores", "Windows не спрашивает DNS всех адаптеров", "Windows tidak menanyakan DNS semua adapter");
            Add("dns.leak",
                "DNS leak: {0}", "DNS 泄漏：{0}", "DNS रिसाव: {0}", "Fuga de DNS: {0}", "تسرب DNS: {0}", "Fuite DNS : {0}", "DNS ফাঁস: {0}", "Vazamento de DNS: {0}", "Утечка DNS: {0}", "Kebocoran DNS: {0}");
            Add("dns.silent",
                "The other adapter’s DNS did not answer", "另一个适配器的 DNS 没有响应", "दूसरे अडैप्टर के DNS ने उत्तर नहीं दिया", "El DNS del otro adaptador no respondió", "DNS المحول الآخر لم يجب", "Le DNS de l’autre adaptateur n’a pas répondu", "অন্য অ্যাডাপ্টারের DNS উত্তর দেয়নি", "O DNS do outro adaptador não respondeu", "DNS другого адаптера не ответил", "DNS adapter lain tidak menjawab");
            Add("dns.clean",
                "No DNS leak", "没有 DNS 泄漏", "DNS रिसाव नहीं है", "No hay fuga de DNS", "لا يوجد تسرب DNS", "Pas de fuite DNS", "DNS ফাঁস নেই", "Não há vazamento de DNS", "Утечки DNS нет", "Tidak ada kebocoran DNS");
            Add("dns.noadapter",
                "Could not find the main network adapter", "找不到主要网络适配器", "मुख्य नेटवर्क अडैप्टर नहीं मिला", "No se encontró el adaptador de red principal", "تعذر العثور على محول الشبكة الرئيسي", "Adaptateur réseau principal introuvable", "প্রধান নেটওয়ার্ক অ্যাডাপ্টার পাওয়া যায়নি", "Não foi encontrado o adaptador de rede principal", "Не нашёл основной сетевой адаптер", "Adapter jaringan utama tidak ditemukan");
            Add("dns.dry.apply",
                "Preview: the system was not changed.", "预览：系统未更改。", "पूर्वावलोकन: सिस्टम नहीं बदला।", "Vista previa: el sistema no cambió.", "معاينة: لم يتغير النظام.", "Aperçu : le système n’a pas changé.", "প্রিভিউ: সিস্টেম বদলায়নি।", "Prévia: o sistema não foi alterado.", "Просмотр: систему не менял.", "Pratinjau: sistem tidak diubah.");
            Add("dns.fixed",
                "Extra DNS is off. {0}", "多余的 DNS 已关闭。{0}", "अतिरिक्त DNS बंद है। {0}", "El DNS extra está apagado. {0}", "أُوقف DNS الزائد. {0}", "Le DNS en trop est coupé. {0}", "অতিরিক্ত DNS বন্ধ। {0}", "O DNS extra está desligado. {0}", "Лишний DNS выключен. {0}", "DNS ekstra dimatikan. {0}");
            Add("dns.watch.off",
                "Watching is off. The Windows setting was not changed.", "监视已关闭。Windows 的设置没有改。", "निगरानी बंद है। Windows की सेटिंग नहीं बदली।", "La vigilancia está apagada. El ajuste de Windows no cambió.", "المراقبة متوقفة. إعداد Windows لم يتغير.", "La surveillance est coupée. Le réglage Windows n’a pas changé.", "নজরদারি বন্ধ। Windows-এর সেটিং বদলায়নি।", "A vigilância está desligada. O ajuste do Windows não mudou.", "Слежение выключено. Настройку Windows не менял.", "Pemantauan mati. Pengaturan Windows tidak diubah.");
            Add("dns.home.none",
                "Home network. DNS protection was not turned on.", "家庭网络。DNS 保护没有打开过。", "घरेलू नेटवर्क। DNS सुरक्षा चालू नहीं थी।", "Red de casa. La protección DNS no estaba activada.", "شبكة منزلية. حماية DNS لم تكن مفعّلة.", "Réseau de la maison. La protection DNS n’était pas activée.", "বাড়ির নেটওয়ার্ক। DNS সুরক্ষা চালু ছিল না।", "Rede de casa. A proteção de DNS não estava ligada.", "Домашняя сеть. Защиту DNS не включали.", "Jaringan rumah. Perlindungan DNS tidak dihidupkan.");
            Add("dns.dry.home",
                "Preview: on the home network the previous DNS would be restored.", "预览：在家庭网络上会恢复原来的 DNS。", "पूर्वावलोकन: घरेलू नेटवर्क पर पिछला DNS वापस होता।", "Vista previa: en la red de casa se devolvería el DNS anterior.", "معاينة: على الشبكة المنزلية كان سيُعاد DNS السابق.", "Aperçu : sur le réseau de la maison l’ancien DNS serait remis.", "প্রিভিউ: বাড়ির নেটওয়ার্কে আগের DNS ফিরত।", "Prévia: na rede de casa o DNS anterior seria restaurado.", "Просмотр: на домашней сети вернул бы прежний DNS.", "Pratinjau: di jaringan rumah DNS sebelumnya akan dikembalikan.");
            Add("dns.dry.revert",
                "Preview: the previous DNS setting would be restored.", "预览：会恢复原来的 DNS 设置。", "पूर्वावलोकन: पिछली DNS सेटिंग वापस होती।", "Vista previa: se devolvería el ajuste DNS anterior.", "معاينة: كان سيُعاد إعداد DNS السابق.", "Aperçu : l’ancien réglage DNS serait remis.", "প্রিভিউ: আগের DNS সেটিং ফিরত।", "Prévia: o ajuste de DNS anterior seria restaurado.", "Просмотр: вернул бы прежнюю настройку DNS.", "Pratinjau: pengaturan DNS sebelumnya akan dikembalikan.");
            Add("dns.again.watch",
                "On the next VPN I will fix it myself.", "下次连上 VPN 时我会自己修复。", "अगले VPN पर मैं खुद ठीक कर दूँगा।", "En la próxima VPN lo corregiré solo.", "في VPN التالي سأصلح من نفسي.", "Au prochain VPN je corrigerai tout seul.", "পরের VPN-এ আমি নিজে ঠিক করব।", "Na próxima VPN eu corrijo sozinho.", "При следующем VPN исправлю сам.", "Pada VPN berikutnya saya perbaiki sendiri.");
            Add("dns.again.once",
                "On the next VPN I will not check by myself.", "下次连上 VPN 时我不会自己再查。", "अगले VPN पर मैं खुद नहीं जाँचूँगा।", "En la próxima VPN no comprobaré solo.", "في VPN التالي لن أتحقق من نفسي.", "Au prochain VPN je ne vérifierai pas tout seul.", "পরের VPN-এ আমি নিজে পরীক্ষা করব না।", "Na próxima VPN eu não verifico sozinho.", "При следующем VPN само уже не проверю.", "Pada VPN berikutnya saya tidak memeriksa sendiri.");
            Add("dns.home.restored",
                "Home network. Previous DNS restored. {0}", "家庭网络。原来的 DNS 已恢复。{0}", "घरेलू नेटवर्क। पिछला DNS वापस हो गया। {0}", "Red de casa. DNS anterior restaurado. {0}", "شبكة منزلية. أُعيد DNS السابق. {0}", "Réseau de la maison. Ancien DNS remis. {0}", "বাড়ির নেটওয়ার্ক। আগের DNS ফিরে এসেছে। {0}", "Rede de casa. DNS anterior restaurado. {0}", "Домашняя сеть. Вернул прежний DNS. {0}", "Jaringan rumah. DNS sebelumnya dikembalikan. {0}");
            Add("dns.reverted",
                "Watching is off. The previous DNS setting was restored.", "监视已关闭。原来的 DNS 设置已恢复。", "निगरानी बंद है। पिछली DNS सेटिंग वापस हो गई।", "La vigilancia está apagada. Se restauró el ajuste DNS anterior.", "المراقبة متوقفة. أُعيد إعداد DNS السابق.", "La surveillance est coupée. L’ancien réglage DNS a été remis.", "নজরদারি বন্ধ। আগের DNS সেটিং ফিরে এসেছে।", "A vigilância está desligada. O ajuste de DNS anterior foi restaurado.", "Слежение выключено. Вернул прежнюю настройку DNS.", "Pemantauan mati. Pengaturan DNS sebelumnya dikembalikan.");
            Add("dns.restart.partial",
                "The DNS service was restarted, the network service was not. If the internet is gone, restart the computer.", "DNS 服务已重启，网络服务没有。如果上不了网，请重启电脑。", "DNS सेवा फिर चली, नेटवर्क सेवा नहीं। अगर इंटरनेट गया है तो कंप्यूटर पुनरारंभ करें।", "El servicio DNS se reinició, el de red no. Si no hay internet, reinicie el equipo.", "أُعيد تشغيل خدمة DNS لا خدمة الشبكة. إذا انقطع الإنترنت فأعد تشغيل الجهاز.", "Le service DNS a redémarré, pas le service réseau. Si internet a disparu, redémarrez l’ordinateur.", "DNS সেবা আবার চালু হয়েছে, নেটওয়ার্ক সেবা হয়নি। ইন্টারনেট না থাকলে কম্পিউটার পুনরায় চালু করুন।", "O serviço de DNS reiniciou, o de rede não. Se a internet sumiu, reinicie o computador.", "Служба DNS перезапущена, служба сети — нет. Если пропал интернет, перезагрузите компьютер.", "Layanan DNS dimulai ulang, layanan jaringan tidak. Jika internet hilang, mulai ulang komputer.");
            Add("dns.restart.ok",
                "The DNS service was restarted.", "DNS 服务已重启。", "DNS सेवा फिर चल पड़ी।", "El servicio DNS se reinició.", "أُعيد تشغيل خدمة DNS.", "Le service DNS a redémarré.", "DNS সেবা আবার চালু হয়েছে।", "O serviço de DNS foi reiniciado.", "Служба DNS перезапущена.", "Layanan DNS dimulai ulang.");
            Add("dns.restart.reboot",
                "The setting is saved. It will take effect after a restart.", "设置已保存。重启后才会生效。", "सेटिंग सहेज ली गई। पुनरारंभ के बाद लागू होगी।", "El ajuste está guardado. Tendrá efecto tras reiniciar.", "حُفظ الإعداد. يعمل بعد إعادة التشغيل.", "Le réglage est enregistré. Il prendra effet après un redémarrage.", "সেটিং সংরক্ষিত। পুনরায় চালু করলে কার্যকর হবে।", "O ajuste foi salvo. Vale depois de reiniciar.", "Настройка записана. Заработает после перезагрузки.", "Pengaturan tersimpan. Berlaku setelah komputer dimulai ulang.");
            Add("dns.norestore",
                "Could not restore the adapter DNS. The backup list is kept.", "未能恢复适配器的 DNS。备用列表已保留。", "अडैप्टर का DNS वापस नहीं हो सका। बैकअप सूची रखी है।", "No se pudo restaurar el DNS del adaptador. La lista de reserva se conserva.", "تعذر إرجاع DNS المحول. قائمة الاحتياط محفوظة.", "Impossible de remettre le DNS de l’adaptateur. La liste de secours est gardée.", "অ্যাডাপ্টারের DNS ফেরানো যায়নি। ব্যাকআপ তালিকা রাখা আছে।", "Não foi possível restaurar o DNS do adaptador. A lista reserva foi mantida.", "Не удалось вернуть DNS адаптера. Запасной список сохранён.", "Tidak dapat mengembalikan DNS adapter. Daftar cadangan tetap disimpan.");
            Add("dns.nokey",
                "DNS registry key is missing", "缺少 DNS 注册表项", "DNS रजिस्ट्री कुंजी नहीं है", "Falta la clave de registro DNS", "مفتاح سجل DNS غير موجود", "La clé de registre DNS est absente", "DNS রেজিস্ট্রি কী নেই", "A chave de registro DNS está ausente", "Нет ключа реестра DNS", "Kunci registri DNS tidak ada");
            Add("dns.parallel.on",
                "Parallel lookup: on", "并行查询：开", "समानांतर जाँच: चालू", "Consulta en paralelo: activada", "الاستعلام المتوازي: يعمل", "Requête parallèle : activée", "সমান্তরাল অনুসন্ধান: চালু", "Consulta paralela: ligada", "Параллельный опрос: включён", "Pencarian paralel: nyala");
            Add("dns.parallel.off",
                "Parallel lookup: off", "并行查询：关", "समानांतर जाँच: बंद", "Consulta en paralelo: desactivada", "الاستعلام المتوازي: متوقف", "Requête parallèle : désactivée", "সমান্তরাল অনুসন্ধান: বন্ধ", "Consulta paralela: desligada", "Параллельный опрос: выключен", "Pencarian paralel: mati");
            Add("dns.adapter",
                "Main adapter: {0}", "主要适配器：{0}", "मुख्य अडैप्टर: {0}", "Adaptador principal: {0}", "المحول الرئيسي: {0}", "Adaptateur principal : {0}", "প্রধান অ্যাডাপ্টার: {0}", "Adaptador principal: {0}", "Основной адаптер: {0}", "Adapter utama: {0}");
            Add("dns.answer",
                ": answered", "：有响应", ": उत्तर मिला", ": respondió", ": أجاب", " : a répondu", ": উত্তর দিয়েছে", ": respondeu", ": ответ есть", ": menjawab");
            Add("dns.quiet",
                ": no answer", "：无响应", ": कोई उत्तर नहीं", ": no respondió", ": لا جواب", " : pas de réponse", ": উত্তর নেই", ": sem resposta", ": молчит", ": tidak menjawab");

            Add("tz.missing",
                "Windows has no time zone {0}", "Windows 没有时区 {0}", "Windows में समय क्षेत्र {0} नहीं है", "Windows no tiene el huso {0}", "Windows ليس فيه المنطقة {0}", "Windows n’a pas le fuseau {0}", "Windows-এ সময় অঞ্চল {0} নেই", "O Windows não tem o fuso {0}", "В Windows нет пояса {0}", "Windows tidak punya zona {0}");
            Add("tz.noadmin",
                "No administrator rights, the time zone was not changed", "没有管理员权限，时区未更改", "व्यवस्थापक अधिकार नहीं, समय क्षेत्र नहीं बदला", "Sin permisos de administrador, el huso no cambió", "لا صلاحية مسؤول، لم تتغير المنطقة", "Pas de droits d’administrateur, le fuseau n’a pas changé", "প্রশাসকের অধিকার নেই, সময় অঞ্চল বদলায়নি", "Sem direitos de administrador, o fuso não mudou", "Нет прав администратора, пояс не изменён", "Tidak ada hak administrator, zona tidak diubah");
            Add("tz.notzutil",
                "tzutil did not start", "tzutil 没有启动", "tzutil शुरू नहीं हुआ", "tzutil no se inició", "لم يبدأ tzutil", "tzutil n’a pas démarré", "tzutil চালু হয়নি", "tzutil não iniciou", "Не запустился tzutil", "tzutil tidak mulai");
            Add("tz.hang",
                "tzutil did not finish", "tzutil 没有结束", "tzutil पूरा नहीं हुआ", "tzutil no terminó", "لم ينتهِ tzutil", "tzutil n’a pas fini", "tzutil শেষ হয়নি", "tzutil não terminou", "tzutil не завершился", "tzutil tidak selesai");
            Add("tz.code",
                "tzutil returned code {0}", "tzutil 返回代码 {0}", "tzutil ने कोड {0} लौटाया", "tzutil devolvió el código {0}", "أعاد tzutil الرمز {0}", "tzutil a renvoyé le code {0}", "tzutil কোড {0} দিয়েছে", "tzutil devolveu o código {0}", "tzutil вернул код {0}", "tzutil mengembalikan kode {0}");
            Add("tz.stuck",
                "After tzutil the time zone is still {0}", "tzutil 之后时区仍是 {0}", "tzutil के बाद समय क्षेत्र अभी भी {0} है", "Tras tzutil el huso sigue siendo {0}", "بعد tzutil المنطقة ما زالت {0}", "Après tzutil le fuseau est encore {0}", "tzutil-এর পরে সময় অঞ্চল এখনও {0}", "Depois do tzutil o fuso ainda é {0}", "После tzutil пояс остался {0}", "Setelah tzutil zona masih {0}");
            Add("task.desc",
                "Adjust the Windows time zone from the public IP", "按公网 IP 调整 Windows 时区", "सार्वजनिक IP से Windows का समय क्षेत्र समायोजित करें", "Ajustar el huso de Windows según la IP pública", "ضبط منطقة Windows حسب IP العام", "Ajuster le fuseau Windows d’après l’IP publique", "সর্বজনীন IP থেকে Windows-এর সময় অঞ্চল ঠিক করুন", "Ajustar o fuso do Windows pelo IP público", "Подстройка часового пояса Windows по внешнему IP", "Sesuaikan zona Windows dari IP publik");

            Add("cli.place", "Place: {0}", "地点：{0}", "स्थान: {0}", "Lugar: {0}", "المكان: {0}", "Lieu : {0}", "স্থান: {0}", "Lugar: {0}", "Место: {0}", "Tempat: {0}");
            Add("cli.iana", "IANA: {0}", "IANA：{0}", "IANA: {0}", "IANA: {0}", "IANA: {0}", "IANA : {0}", "IANA: {0}", "IANA: {0}", "IANA: {0}", "IANA: {0}");
            Add("cli.win", "Windows time zone: {0}", "Windows 时区：{0}", "Windows समय क्षेत्र: {0}", "Huso de Windows: {0}", "منطقة Windows: {0}", "Fuseau Windows : {0}", "Windows সময় অঞ্চল: {0}", "Fuso do Windows: {0}", "Пояс Windows: {0}", "Zona Windows: {0}");
            Add("cli.source", "Source: {0}", "来源：{0}", "स्रोत: {0}", "Fuente: {0}", "المصدر: {0}", "Source : {0}", "উৎস: {0}", "Fonte: {0}", "Источник: {0}", "Sumber: {0}");
            Add("cli.now", "Current on the system: {0}", "系统当前：{0}", "सिस्टम में अभी: {0}", "Ahora en el sistema: {0}", "الحالي في النظام: {0}", "Actuellement sur le système : {0}", "সিস্টেমে এখন: {0}", "Atual no sistema: {0}", "Сейчас в системе: {0}", "Saat ini di sistem: {0}");
            Add("cli.same", "No change needed.", "无需更改。", "बदलाव की ज़रूरत नहीं।", "No hace falta cambiar.", "لا حاجة للتغيير.", "Aucun changement nécessaire.", "বদলানোর দরকার নেই।", "Nenhuma mudança necessária.", "Смена не нужна.", "Tidak perlu diubah.");
            Add("cli.diff",
                "The system time zone differs from the IP time zone. Starting without --check will set it.",
                "系统时区与 IP 时区不同。不带 --check 启动就会设置它。",
                "सिस्टम का समय क्षेत्र IP वाले से अलग है। --check के बिना चलाने पर वह लग जाएगा।",
                "El huso del sistema difiere del de la IP. Al iniciar sin --check se pondrá.",
                "منطقة النظام تختلف عن منطقة IP. التشغيل دون --check يضبطها.",
                "Le fuseau du système diffère de celui de l’IP. Un lancement sans --check le définira.",
                "সিস্টেমের সময় অঞ্চল IP-এর থেকে আলাদা। --check ছাড়া চালালে সেটি বসবে।",
                "O fuso do sistema difere do fuso do IP. Iniciar sem --check vai defini-lo.",
                "Пояс системы отличается от пояса IP. Запуск без --check его поставит.",
                "Zona sistem berbeda dari zona IP. Menjalankan tanpa --check akan mengaturnya.");
            Add("cli.nomap",
                "no match: {0}", "没有对应：{0}", "कोई मेल नहीं: {0}", "sin correspondencia: {0}", "لا يوجد تطابق: {0}", "pas de correspondance : {0}", "কোনো মিল নেই: {0}", "sem correspondência: {0}", "нет соответствия: {0}", "tidak ada padanan: {0}");

            Add("zone.title",
                "Time zone for this IP", "此 IP 的时区", "इस IP का समय क्षेत्र", "Huso para esta IP", "المنطقة لهذا IP", "Fuseau pour cette IP", "এই IP-এর সময় অঞ্চল", "Fuso para este IP", "Пояс для этого IP", "Zona untuk IP ini");
            Add("zone.pin",
                "Pin to this IP", "固定到此 IP", "इस IP पर पिन करें", "Fijar a esta IP", "ثبّت على هذا IP", "Épingler à cette IP", "এই IP-তে পিন করুন", "Fixar neste IP", "Закрепить за этим IP", "Sematkan ke IP ini");
            Add("zone.hint",
                "While it is pinned, lookup by IP does not change this time zone.", "固定期间，按 IP 的判断不会改这个时区。", "जब तक पिन है, IP से पता लगाना इस समय क्षेत्र को नहीं बदलता।", "Mientras esté fijado, la detección por IP no cambia este huso.", "ما دام مثبتاً، التحديد حسب IP لا يغير هذه المنطقة.", "Tant que c’est épinglé, la détection par IP ne change pas ce fuseau.", "পিন থাকা পর্যন্ত IP দিয়ে খোঁজা এই সময় অঞ্চল বদলায় না।", "Enquanto estiver fixado, a detecção pelo IP não muda este fuso.", "Пока закреплено, определение по IP этот пояс не меняет.", "Selama disematkan, pencarian lewat IP tidak mengubah zona ini.");
            Add("zone.apply",
                "Apply", "应用", "लागू करें", "Aplicar", "تطبيق", "Appliquer", "প্রয়োগ", "Aplicar", "Применить", "Terapkan");
        }

        public static void Use(string stored)
        {
            string code;
            if (string.IsNullOrEmpty(stored) || string.Equals(stored, "auto", StringComparison.OrdinalIgnoreCase))
                code = Detect();
            else
                code = Canonical(stored);
            if (string.IsNullOrEmpty(code)) code = "en";
            Code = code;
            _index = 0;
            for (int i = 0; i < Order.Length; i++)
            {
                if (Order[i] == code) { _index = i; break; }
            }
            if (_ownFont && _font != null) _font.Dispose();
            _font = null;
            _ownFont = false;
        }

        public static bool Known(string stored)
        {
            if (string.IsNullOrEmpty(stored) || string.Equals(stored, "auto", StringComparison.OrdinalIgnoreCase)) return true;
            return Canonical(stored) != null;
        }

        public static string Detect()
        {
            string name = PreferredUi();
            if (string.IsNullOrEmpty(name)) name = FromLcid(GetUserDefaultUILanguage());
            if (string.IsNullOrEmpty(name))
            {
                try { name = CultureInfo.CurrentUICulture.Name; } catch { }
            }
            if (string.IsNullOrEmpty(name))
            {
                try { name = CultureInfo.CurrentCulture.Name; } catch { }
            }
            string code = Canonical(name);
            return string.IsNullOrEmpty(code) ? "en" : code;
        }

        public static string ChoiceLabel(string code)
        {
            if (string.IsNullOrEmpty(code)) return "Windows · " + Native(Detect());
            return Native(code);
        }

        public static string T(string key)
        {
            string[] row;
            if (!Rows.TryGetValue(key, out row) || row == null || row.Length <= _index) return key;
            string text = row[_index];
            if (string.IsNullOrEmpty(text) && row.Length > 0) text = row[0];
            return string.IsNullOrEmpty(text) ? key : text;
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, T(key), args);
        }

        public static Font UiFont
        {
            get
            {
                if (_font != null) return _font;
                string family = FamilyFor(Code);
                if (family == null)
                {
                    _font = SystemFonts.MessageBoxFont;
                    _ownFont = false;
                }
                else
                {
                    _font = new Font(family, SystemFonts.MessageBoxFont.SizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
                    _ownFont = true;
                }
                return _font;
            }
        }

        public static void KeepLeftToRight()
        {
            try { SetProcessDefaultLayout(0); } catch { }
        }

        static string Canonical(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string s = name.Trim().Replace('_', '-').ToLowerInvariant();
            int dash = s.IndexOf('-');
            string head = dash < 0 ? s : s.Substring(0, dash);
            if (head == "zh" || head == "pt" || head == "hi" || head == "es" || head == "ar"
                || head == "fr" || head == "bn" || head == "ru" || head == "id" || head == "en")
                return head;
            return null;
        }

        static string Native(string code)
        {
            if (code == "zh") return "中文";
            if (code == "hi") return "हिन्दी";
            if (code == "es") return "Español";
            if (code == "ar") return "العربية";
            if (code == "fr") return "Français";
            if (code == "bn") return "বাংলা";
            if (code == "pt") return "Português";
            if (code == "ru") return "Русский";
            if (code == "id") return "Bahasa Indonesia";
            return "English";
        }

        static string PreferredUi()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                {
                    if (key == null) return null;
                    object value = key.GetValue("PreferredUILanguages");
                    string[] list = value as string[];
                    if (list != null && list.Length > 0 && !string.IsNullOrEmpty(list[0])) return list[0];
                    return value as string;
                }
            }
            catch
            {
                return null;
            }
        }

        static string FromLcid(ushort lcid)
        {
            if (lcid == 0) return null;
            try { return CultureInfo.GetCultureInfo((int)lcid).Name; }
            catch { return null; }
        }

        static string FamilyFor(string code)
        {
            if (code == "zh") return FirstFont("Microsoft YaHei UI", "Microsoft YaHei", "SimSun", "Segoe UI");
            if (code == "hi" || code == "bn") return FirstFont("Nirmala UI", "Segoe UI");
            if (code == "ar") return FirstFont("Segoe UI", "Tahoma");
            return null;
        }

        static string FirstFont(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                using (var probe = new Font(names[i], 9f, FontStyle.Regular, GraphicsUnit.Point))
                {
                    if (string.Equals(probe.FontFamily.Name, names[i], StringComparison.OrdinalIgnoreCase))
                        return names[i];
                }
            }
            return null;
        }

        static void Add(string key, params string[] row)
        {
            if (row == null || row.Length != 10)
                throw new InvalidOperationException(key);
            for (int i = 0; i < row.Length; i++)
            {
                if (string.IsNullOrEmpty(row[i])) throw new InvalidOperationException(key);
            }
            Rows[key] = row;
        }

        [DllImport("kernel32.dll")]
        static extern ushort GetUserDefaultUILanguage();

        [DllImport("user32.dll")]
        static extern bool SetProcessDefaultLayout(uint dwDefaultLayout);
    }
}
