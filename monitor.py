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
fullTemperatureImageFileName = '/share/raspiEyes/full_temperatures.png'
humidityFileName = '/share/raspiEyes/humidities.txt'
humidityImageFileName = '/share/raspiEyes/humidities.png'
captureImageFileName = '/share/raspiEyes/capture.jpg'
gitRepoPath = '/share/raspiEyes/'
gitCommitMessage = f'{now.strftime("%Y-%m-%d %H:%M")}'
mapFileName = '/share/raspiEyes/map.jpg'
routeFileName = '/share/raspiEyes/route.jpg'
coordinatesFileName = '/share/raspiEyes/coordinates.txt'
maxDataItems = 30

# pir = MotionSensor(4)
if cameraCaptureIsOn:
	camera = PiCamera()
	camera.resolution = (1920, 1080)
	camera.start_preview()
	sleep(5)
	camera.capture(captureImageFileName)
	camera.stop_preview()

# Attempt to get a sensor reading. The read_retry method will
# retry up to 15 times, waiting 2 seconds between attempts
sensormodel = Adafruit_DHT.AM2302
sensorpin = 4
humidity, temperature = Adafruit_DHT.read_retry(sensormodel, sensorpin)

print(f'current humidity: {humidity})
print(f'current temperature: {temperature})

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

fullTemperatureTimeList = []
fullTemperatureDataList = []
i = 0
for e in temperatureContent:
	if i == 0 or i == len(list(temperatureContent)) - 1:
		fullTemperatureTimeList.append(e.split(',')[0])
	else:
		fullTemperatureTimeList.append('')
	fullTemperatureDataList.append(float(e.split(',')[1]))
	i = i + 1

fullTemperatureTimeList = list(reversed(fullTemperatureTimeList))
fullTemperatureDataList = list(reversed(fullTemperatureDataList))

temperatureTimeList = []
temperatureDataList = []
i = 0
for e in temperatureContent:
	if i < maxDataItems:
		temperatureTimeList.append(e.split(',')[0])
		temperatureDataList.append(float(e.split(',')[1]))
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

plt.plot(fullTemperatureTimeList, fullTemperatureDataList)
plt.ylabel('Degrees Celsius')
plt.xlabel('Date')
plt.title('Temperature')
plt.xticks(rotation=90)
plt.grid(True)
plt.savefig(fullTemperatureImageFileName, bbox_inches='tight')
plt.close()

img = Image.open(temperatureImageFileName)
ImageDraw.Draw(
    img  # Image
).text(
    (0, 0),  # Coordinates
    gitCommitMessage,  # Text
    (0, 0, 0)  # Color
)
img.save(temperatureImageFileName)

img = Image.open(fullTemperatureImageFileName)
ImageDraw.Draw(
    img  # Image
).text(
    (0, 0),  # Coordinates
    gitCommitMessage,  # Text
    (0, 0, 0)  # Color
)
img.save(fullTemperatureImageFileName)

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

img = Image.open(humidityImageFileName)
ImageDraw.Draw(
    img  # Image
).text(
    (0, 0),  # Coordinates
    gitCommitMessage,  # Text
    (0, 0, 0)  # Color
)
img.save(humidityImageFileName)

with open(coordinatesFileName, 'r') as file:
	coordinatesContent = file.readlines()

# you may also want to remove whitespace characters like `\n` at the end of each line
coordinatesContent = [x.strip() for x in coordinatesContent]
# coordinatesContent = reversed(coordinatesContent)
coordinatesTimeList = []
coordinatesLatList = []
coordinatesLongList = []
i = 0
_lat = 0
_long = 0
for e in coordinatesContent:
	if e != '':
		# print(e)
		newLat = float(e.split(',')[1])
		newLong = float(e.split(',')[2])
		if newLat != _lat or newLong != _long:
			coordinatesTimeList.append(e.split(',')[0])
			coordinatesLatList.append(newLat)
			coordinatesLongList.append(newLong)
			_lat = newLat
			_long = newLong
			i = i + 1

coordinatesTimeList = list(reversed(coordinatesTimeList))
coordinatesLatList = list(reversed(coordinatesLatList))
coordinatesLongList = list(reversed(coordinatesLongList))
# https://www.mapquestapi.com/staticmap/v4/getmap?size=1200,1200&type=map&zoom=8&center=58.625555,16.34823&mcenter=58.625555,16.34823&imagetype=JPEG&key=27OtkDxArEqki7qITqKQbtPgfAtHaWOe&shape=raw&polyline=fill:0x70ff0000|color:0xff0000|width:2|58.625555,16.34823
# https://www.mapquestapi.com/staticmap/v5/map?key=27OtkDxArEqki7qITqKQbtPgfAtHaWOe&shape&center=58.625555,16.34823&size=1000,1000&type=hyb&locations=58.625555,16.34823&shape=58.625555,16.34823|58.625555,16.3481783333333
_lat = coordinatesLatList[0]
_long = coordinatesLongList[0]
print(f'{_lat},{_long}')
mapUrl = f'https://www.mapquestapi.com/staticmap/v5/map?key=27OtkDxArEqki7qITqKQbtPgfAtHaWOe&shape&size=1000,1000&type=hyb&locations={_lat},{_long}'
print(mapUrl)
with open(mapFileName, 'wb') as f:
	f.write(requests.get(mapUrl).content)


img = Image.open(mapFileName)
ImageDraw.Draw(
    img  # Image
).text(
    (0, 0),  # Coordinates
    gitCommitMessage,  # Text
    (0, 0, 0)  # Color
)
img.save(mapFileName)

routeUrl = f'https://www.mapquestapi.com/staticmap/v5/map?key=27OtkDxArEqki7qITqKQbtPgfAtHaWOe&shape&size=1500,1500&locations={_lat},{_long}&shape='
i = 0
for e in coordinatesTimeList:
	routeUrl = f'{routeUrl}{coordinatesLatList[i]},{coordinatesLongList[i]}|'
	i = i + 1

print(routeUrl)
with open(routeFileName, 'wb') as f:
	f.write(requests.get(routeUrl).content)


img = Image.open(routeFileName)
ImageDraw.Draw(
    img  # Image
).text(
    (0, 0),  # Coordinates
    gitCommitMessage,  # Text
    (0, 0, 0)  # Color
)
img.save(routeFileName)


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
