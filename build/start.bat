@echo off
chcp 65001 >nul
start "" ".\FilteringUtility.exe" ^
    _cityDistrict=Юг ^
    _firstDeliveryDateTimeStart=2024-10-26T10:10:00 ^
    _firstDeliveryDateTimeEnd=2024-10-26T19:20:00 ^
    _deliveryLog=.\ ^
    _deliveryOrder=.\deliveryOrders.txt ^
    _dataPath=.\Data.csv
