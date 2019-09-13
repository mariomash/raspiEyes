# Hymer Exsis Travel :-)
## Realtime Location & Data

![](map.jpg?raw=true)
![](temperatures.png?raw=true)
![](humidities.png?raw=true)
![](route.jpg?raw=true)
![](capture.jpg?raw=true)
![](exsis.jpeg?raw=true)

## Some instructions
apply this: https://www.raspberrypi.org/magpi/samba-file-server/

wifi_rebooter.sh contents

```
#!/bin/bash

# The IP for the server you wish to ping (8.8.8.8 is a public Google DNS server)
SERVER=8.8.8.8

# Only send two pings, sending output to /dev/null
ping -c2 ${SERVER} > /dev/null

# If the return code from ping ($?) is not 0 (meaning there was an error)
if [ $? != 0 ]
then
    # Restart the wireless interface
    ifconfig wlan0 down
    ifconfig wlan0 up
fi
```

```
sudo nano /etc/crontab
*/1 *   * * *   root    /share/wifi_rebooter.sh
30 *   * * *   root    /usr/bin/python3 '/share/raspiEyes/monitor.py' 1> /share/monitor.out.txt 2> /share/monitor.err.txt

sudo apt-get install python3-matplotlib
sudo apt-get install python3-git
sudo apt-get install python3-picamera
sudo pip3 install Adafruit_DHT
chmod a+x monitor.py
```
