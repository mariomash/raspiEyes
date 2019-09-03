# raspiEyes
![](map.jpg?raw=true)
![](temperatures.png?raw=true)
![](humidities.png?raw=true)
![](capture.jpg?raw=true)

# Instructions
```
sudo nano /etc/crontab
```
```
*/1 *   * * *   root    /share/wifi_rebooter.sh
*/1 *   * * *   root    /usr/bin/python3 '/share/raspiEyes/monitor.py' 1> /share/monitor.out.txt 2> /share/monitor.err.txt
```