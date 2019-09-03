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
import random
from PIL import Image
from PIL import ImageFont
from PIL import ImageDraw 

cameraCaptureIsOn = False
now = datetime.datetime.now()
temperatureFileName = '/share/raspiEyes/temperatures.txt'
temperatureImageFileName = '/share/raspiEyes/temperatures.png'
humidityFileName = '/share/raspiEyes/humidities.txt'
humidityImageFileName = '/share/raspiEyes/humidities.png'
captureImageFileName = '/share/raspiEyes/capture.jpg'
gitRepoPath = '/share/raspiEyes/'
gitCommitMessage = f'{now.strftime("%Y-%m-%d %H:%M")}'
mapFileName = '/share/raspiEyes/map.jpg'
coordinatesFileName = '/share/raspiEyes/coordinates.txt'
maxDataItems = 30

# pir = MotionSensor(4)
if cameraCaptureIsOn:
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

# humidity = round(humidity, 2)
# temperature = round(temperature, 2)

# Prepare temperature file
with open(temperatureFileName, 'a') as file:
	file.write(f'{now.strftime("%Y-%m-%d %H:%M")},{temperature}\r\n')

with open(temperatureFileName, 'r') as file:
	temperatureContent = file.readlines()

# you may also want to remove whitespace characters like `\n` at the end of each line
temperatureContent = [x.strip() for x in temperatureContent]
temperatureContent = reversed(temperatureContent)
temperatureTimeList = []
temperatureDataList = []
i = 0
for e in temperatureContent:
	if i < maxDataItems:
		temperatureTimeList.append(e.split(',')[0])
		temperatureDataList.append(float(e.split(',')[1]))
		#temperatureDataList.append(random.random())
		i = i + 1
temperatureTimeList = list(reversed(temperatureTimeList))
temperatureDataList = list(reversed(temperatureDataList))
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
humidityContent = reversed(humidityContent)
humidityTimeList = []
humidityDataList = []
i = 0
for e in humidityContent:
	if i < maxDataItems:
		humidityTimeList.append(e.split(',')[0])
		humidityDataList.append(float(e.split(',')[1]))
		i = i + 1

#print(humidityDataList)
humidityTimeList = list(reversed(humidityTimeList))
humidityDataList = list(reversed(humidityDataList))
plt.plot(humidityTimeList, humidityDataList)
plt.ylabel('Humidity Percentage')
plt.xlabel('Date')
plt.title('Humidity')
plt.xticks(rotation=90)
plt.grid(True)
plt.savefig(humidityImageFileName, bbox_inches='tight')
plt.close()

with open(coordinatesFileName, 'r') as file:
	coordinatesContent = file.readlines()

# you may also want to remove whitespace characters like `\n` at the end of each line
coordinatesContent = [x.strip() for x in coordinatesContent]
coordinatesContent = reversed(coordinatesContent)
coordinatesTimeList = []
coordinatesLatList = []
coordinatesLongList = []
i = 0
for e in coordinatesContent:
	if i < 1:
		coordinatesTimeList.append(e.split(',')[0])
		coordinatesLatList.append(float(e.split(',')[1]))
		coordinatesLongList.append(float(e.split(',')[2]))
		i = i + 1
coordinatesTimeList = list(reversed(coordinatesTimeList))
coordinatesLatList = list(reversed(coordinatesLatList))
coordinatesLongList = list(reversed(coordinatesLongList))

lat = coordinatesLatList[0]
long = coordinatesLongList[0]
print(f'{lat},{long}')

with open(mapFileName, 'wb') as f:
	f.write(requests.get(f'https://www.mapquestapi.com/staticmap/v4/getmap?size=600,500&type=map&zoom=7&center={lat},{long}&mcenter={lat},{long}&imagetype=JPEG&key=27OtkDxArEqki7qITqKQbtPgfAtHaWOe').content)


img = Image.open(mapFileName)
ImageDraw.Draw(
    img  # Image
).text(
    (10, 10),  # Coordinates
    gitCommitMessage,  # Text
    (0, 0, 0)  # Color
)
img.save(mapFileName)


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
