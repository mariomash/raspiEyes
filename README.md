# Hymer Exsis Travel :-)
## Realtime Location & Data

![](map.jpg?raw=true)
![](temperatures.png?raw=true)
![](humidities.png?raw=true)
![](route.jpg?raw=true)
![](capture.jpg?raw=true)
![](exsis.jpeg?raw=true)

## Some instructions
```
sudo nano /etc/crontab
*/1 *   * * *   root    /share/wifi_rebooter.sh
*/1 *   * * *   root    /usr/bin/python3 '/share/raspiEyes/monitor.py' 1> /share/monitor.out.txt 2> /share/monitor.err.txt

sudo apt-get install python3-matplotlib
sudo apt-get install python3-git
sudo apt-get install python3-picamera
sudo pip3 install Adafruit_DHT
chmod a+x monitor.py
```
