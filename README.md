Версия 1.2:
Что реализовано:
    - Доступен CLI интерфейс
    - Работает шифровка и дешифровка 
    - изменено использование cli. 
    - добавлены подсказки к командам

Как использовать:
    requirements:
        1. dotnet build
        2. cd bin/Debug/net8.0
    -подсказка:
        - ./stego.exe --help
    - шифрование
        - ./stego.exe encode -i input -o output -m "message"   
    - дешифрование
        - ./stego.exe decode -i input - дешифрование