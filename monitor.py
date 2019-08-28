#!/usr/bin/python3

# monitor.py - For Terrarium Controllers using Adafruit
# DHT sensors, Energenie Pimote sockets, and ThingSpeak.
# MIT license.
# https://www.carnivorousplants.co.uk/resources/raspberry-pi-terrarium-controller/

# Imports
from git import Repo
from gpiozero import Energenie, MotionSensor
from picamera import PiCamera
from matplotlib import pyplot as plt
from time import sleep
import Adafruit_DHT
import requests
import datetime

now = datetime.datetime.now()
temperatureFileName = '/share/raspiEyes/temperatures.txt'
temperatureImageFileName = '/share/raspiEyes/temperatures.png'
humidityFileName = '/share/raspiEyes/humidities.txt'
humidityImageFileName = '/share/raspiEyes/humidities.png'
captureImageFileName = '/share/raspiEyes/capture.jpg'
gitRepoPath = '/share/raspiEyes/'
gitCommitMessage = f'{now.strftime("%Y-%m-%d %H:%M")}'

# pir = MotionSensor(4)

camera = PiCamera()
camera.start_preview()
sleep(5)
camera.capture(captureImageFileName)
camera.stop_preview()

# Attempt to get a sensor reading. The read_retry method will
# retry up to 15 times, waiting 2 seconds between attempts
sensormodel = Adafruit_DHT.AM2302
sensorpin = 4
humidity, temperature = Adafruit_DHT.read_retry(sensormodel, sensorpin)
humidity = round(humidity, 1)
temperature = round(temperature, 1)

# Prepare temperature file
with open(temperatureFileName, 'a') as file:
	file.write(f'{now.strftime("%Y-%m-%d %H:%M")},{temperature}\r\n')

with open(temperatureFileName, 'r') as file:
	temperatureContent = file.readlines()

# you may also want to remove whitespace characters like `\n` at the end of each line
temperatureContent = [x.strip() for x in temperatureContent]

temperatureTimeList = []
temperatureDataList = []
for e in temperatureContent:
	temperatureTimeList.append(e.split(',')[0])
	temperatureDataList.append(e.split(',')[1])

plt.plot(temperatureTimeList, temperatureDataList)
plt.ylabel('Degrees Celsius')
plt.xlabel('Date')
plt.title('Temperature')
plt.xticks(rotation=90)
plt.grid(True)
plt.savefig(temperatureImageFileName, bbox_inches='tight')
plt.close()

# Prepare Humidity File
with open(humidityFileName, 'a') as file:
	file.write(f'{now.strftime("%Y-%m-%d %H:%M")},{humidity}\r\n')

with open(humidityFileName, 'r') as file:
	humidityContent = file.readlines()

# you may also want to remove whitespace characters like `\n` at the end of each line
humidityContent = [x.strip() for x in humidityContent]

humidityTimeList = []
humidityDataList = []
for e in humidityContent:
	humidityTimeList.append(e.split(',')[0])
	humidityDataList.append(e.split(',')[1])

#print(humidityDataList)

humidityTimeList = []
humidityDataList = []
for e in humidityContent:
	humidityTimeList.append(e.split(',')[0])
	humidityDataList.append(e.split(',')[1])

plt.plot(humidityTimeList, humidityDataList)
plt.ylabel('Humidity Percentage')
plt.xlabel('Date')
plt.title('Humidity')
plt.xticks(rotation=90)
plt.grid(True)
plt.savefig(humidityImageFileName, bbox_inches='tight')
plt.close()

# Let's commit
def git_push():
	try:
		repo = Repo(gitRepoPath)
		repo.git.add(update=True)
		repo.index.commit(gitCommitMessage)
		origin = repo.remote(name='origin')
		origin.push()
	except Exception as e:
		print (e)

git_push()

# If either reading has failed after repeated retries,
# abort and log message to ThingSpeak
thingspeak_key = '3PTEOQ651EZUJOAC'
if humidity is None or temperature is None:
    f = requests.post('https://api.thingspeak.com/update.json',
                      data={'api_key': thingspeak_key, 'status': 'failed to get reading'})

# Otherwise, check if temperature is above threshold,
# and if so, activate Energenie socket for cooling fan
else:
    #	fansocket = 1
    #	tempthreshold = 28

    #	if temperature > tempthreshold:
                # Activate cooling fans
    #		f = Energenie(fansocket, initial_value=True)

    #	else:
                # Deactivate cooling fans
    #		f = Energenie(fansocket, initial_value=False)

    # Send the data to Thingspeak
    r = requests.post('https://api.thingspeak.com/update.json',
                      data={'api_key': thingspeak_key, 'field1': temperature, 'field2': humidity})
