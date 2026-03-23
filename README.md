1.1 версия:
Что реализовано:
  - Доступен CLI интерфейс
  - Работает шифровка и дешифровка 

Как использовать:
  - stego.exe encode input output "message"  - шифрование
  - stego.exe decode input - дешифрование

Пример:
  - stego.exe encode input.jpg output.jpg "Hello World"
    - После шифрования выводится "Done"
  - stego.exe decode output.jpg
    - После дешифрования в консоль выводится спрятанный текст
