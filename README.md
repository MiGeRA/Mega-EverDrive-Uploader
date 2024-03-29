# Mega-EverDrive-Uploader

Скорректированная и оптимизированная юзер-френдли сборка **mega-usb** - утилиты управления картриджем [Mega EverDrive](https://krikzz.com/our-products/legacy/megax7.html) (стоковый [исходный код](https://krikzz.com/pub/support/mega-everdrive/x3x5x7/dev/usb-tool/v2.0/) на сайте разработчика Mega EverDrive).

![mega-everdrive](https://user-images.githubusercontent.com/24475390/150523382-7a897bad-1fcc-480d-82ca-3559f1771fc9.jpg)
#### Плюшки:
+ Встроена справочная легенда по формату использования и параметрам (/?);
+ Детализированы сообщения по ходу работы утилиты;
+ Исправлены недочеты, добавлены дополнительные проверки;
+ Исходник сформатирован «по-красоте» ;-)

При подключении Mega EverDrive к ПК (Windows-система) соединение организуется в виде виртуального USB-COM-порта (требуется драйвер). Когда Mega EverDrive уже включен и загружен с подготовленной карты памяти в нем (на экране Сеги "меню") – игры загружаются в одну команду, для загрузки другой игры нужно вернуться обратно в меню (напр. по сбросу). Если SD-карты в Mega EverDrive нет, то сначала загружаем (через USB, данной утилитой) конфигурацию FPGA (*.rbf), потом ОС контроллера (MEGA.BIN) или одной командой враз, а лишь затем игру – далее можно загружать игры также только после сброса "ресетом" (до цикла питания).

Конфигурация SVP.rbf самодостаточна (можно использовать вместо MEGA.rbf для любых игр) – но после ее применения будут также работать загружаемые программы, использующие сопроцессор SVP эмулируемый в FPGA (даже заголовок ROM не учитывается, в то же время при загрузке программы с карты памяти для активации SVP по адресу 0x000150 должно быть написано «Virtua Racing»). Рекомендую использовать комплект версии [3.13](https://krikzz.com/pub/support/mega-everdrive/x3x5x7/OS/) (последняя - 3.14 работает некорректно, т.е. не работает).

По-умолчанию (без опций) образ ROM загружается и запускается как «обычный» (т.е. без маппера – размер до 4МБ); при наличии в заголовке указания на маппер «SSF», он активируется и на Mega EverDrive (вне зависимости от размера образа); если ROM больше 4МБ и не содержит указания SSF в заголовке – то будет активирован режим «M10» («сквозная адресация», которая может быть несовместима с подключенными перманентно Mega-CD или встроенными играми, а также с 32X; режим «M10» используется самоделками и хаками). Указание опций превалирует над указанным порядком. Максимальный размер загружаемого ROM (даже в режиме SSF) – 15МБ (аппаратное ограничение Mega EverDrive).

#### Использовать так:
```
mega-usb <имя_файла> [-опции]

Упрощенное использование (без опций):

*.RBF		- Загрузка конфигурации в FPGA
MEGA.BIN	- Загрузка образа ОС 68k в Mega EverDrive v2(x7), x3 ,x5
MEGAOS.bin	- Загрузка образа ОС 68k в Mega EverDrive v1 (не для v2)
*.SMS		- Загрузка программы (игры) в режиме MasterSystem
*.*		- Загрузка программы (игры) в режиме MegaDrive (SMD, М10, SSF - см. выше)

Опции (не применяются к фиксированным маскам, указанным выше):

-smd		- Загрузка файла как SMD, обычный ROM (зачастую избыточно, т.к. по-умолчанию)
-m10		- Загрузка файла как SMD "big-ROM" (10MB без маппера)
-ssf		- Загрузка файла как SMD ROM с маппером SSF (исп. также для доступа к функциям EverDrive, см. ниже)
-cd		- Загрузка файла как Mega-CD addon ROM BIOS (требуется соотв. оборудование)
-32x		- Загрузка файла как 32X addon ROM (требуется соотв. оборудование)
-sms		- Загрузка файла как MasterSystem ROM
-os		- Загрузка файла как EverDrive приложения
-o		- Загрузка образа ОС в Mega EverDrive v1 (не для v2)
		  (Аналогично MEGAOS.bin без опций)
-fo		- Загрузка образа в т.ч. Прошивки в Mega EverDrive v1 (не для v2)
		  (Часть MEGAOS.bin прошивается, часть загружается)
```
*PS. Не забываем, что Mega EverDrive для разработчиков SMD-приложений предоставляет также следующий функционал:*
* Связь с внешней системой (ПК или другой USB-хост) через виртуальный COM-порт (на стороне хоста) и регистры в адресном пространстве на стороне SMD (необходимо включать режим SSF);
* Возможность хранения контента на SD-карте Mega EverDrive (необходимо включать режим SSF) и его энергонезависимой памяти («на батарейке»);
* Возможность использования адресного пространства памяти ROM картриджа Mega EverDrive как ОЗУ (необходимо включать режим SSF);

(см. примеры разработчика)

* Есть аппаратно-эмулируемый графический сопроцессор SVP (в режиме SVP недоступны регистры SSF, а вместе с этим и функционал EverDrive по работе с USB-соединением и пр.);
* Есть аппаратно-эмулируемый синтезатор YM2413;
* Есть сопроцессор умножения/деления чисел (MEGA.rbf до версии 3.03 включительно).

*PPS. Скомпиллированный, зазипованный экзешник: [mega-usb.exe.zip](https://github.com/MiGeRA/Mega-EverDrive-Uploader/files/9482399/mega-usb.exe.zip)*
