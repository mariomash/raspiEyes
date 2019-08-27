# raspiEyes

- Edit crontab
```
sudo nano /etc/crontab
```
- Add the required line
```
*/1 *   * * *   root    /share/wifi_rebooter.sh
*/1 *   * * *   root    /usr/bin/python3 '/share/raspiEyes/monitor.py' 1> /share/monitor.out.txt 2> /share/monitor.err.txt
```
