#!/usr/bin/python3

# quick_monitor.py - For Terrarium Controllers using Adafruit
# DHT sensors, Energenie Pimote sockets, and ThingSpeak.
# MIT license.
# https://www.carnivorousplants.co.uk/resources/raspberry-pi-terrarium-controller/

# Imports
from git import Repo
import Adafruit_DHT
import requests
import datetime

now = datetime.datetime.now()
lastTemperatureFileName = '/share/raspiEyes/last_temperature.txt'
lastHumidityFileName = '/share/raspiEyes/last_humidity.txt'
gitRepoPath = '/share/raspiEyes/'
gitCommitMessage = f'{now.strftime("%Y-%m-%d %H:%M")}'

# Attempt to get a sensor reading. The read_retry method will
# retry up to 15 times, waiting 2 seconds between attempts
sensormodel = Adafruit_DHT.AM2302
sensorpin = 4
humidity, temperature = Adafruit_DHT.read_retry(sensormodel, sensorpin)

print(f'current humidity: {humidity}')
print(f'current temperature: {temperature}')

with open(lastTemperatureFileName, 'w') as file:
	file.write(f'{now.strftime("%Y-%m-%d %H:%M")},{temperature}\r\n')

with open(lastHumidityFileName, 'w') as file:
	file.write(f'{now.strftime("%Y-%m-%d %H:%M")},{humidity}\r\n')

# Let's commit
# def git_push():
#	try:
#		repo = Repo(gitRepoPath)
#		repo.git.add(update=True)
#		repo.index.commit(gitCommitMessage)
#		origin = repo.remote(name='origin')
#		origin.push()
#	except Exception as e:
#		print (e)

# git_push()
