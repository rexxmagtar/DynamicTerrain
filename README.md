# DynamicTerrain

Было необходимо создание рельефа для движения по нему транспорта. Также стояла задача динамически менять данный рельеф во время игры. Использовал SpriteShape для создания рельефа. В процессе реализации динамического поведения рельефа столкнулся с проблемой слабой производительности (при динамическом изменении сплайна SpriteShape требуется много пересчетов, в т.ч. обновление EdgeCollider2D). В результате работы создал модуль отвечающий за работу динамического рельефа. А так же нашел решения для оптимизации.

## Краткое описание

Изменение рельефа подразумевает смещение точки сплайна spriteshape. Скрипт позволяет:
* Выбрать необходимую точку
* Выбрать конечную точку анимации
* Выбрать кривую скорости анимации
* Ускорить или замедлить анимацию
* Протестировать анимацию


## Пример работы

Редактирование анимаций:

![image](https://user-images.githubusercontent.com/51932532/137226620-10b4d895-23d8-4093-9240-12158cab896e.png)

Демонстрация работы:


https://user-images.githubusercontent.com/51932532/137492698-bdbbdaf9-6df0-4d81-a7e1-6c44859153bf.mov



