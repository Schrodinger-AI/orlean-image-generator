# Orleans MongoDB Setup

Orleans framework need tables and procedures to manage the Grain Operations
Execute the steps listed below:

## MongoDB Setup

### Using docker

#### 1. Start mongo service

using these commands, to start mongodb service (mongodb version: 7.0.6).

```
docker pull mongo
docker run -itd --name mongo -p 27017:27017 -e MONGO_INITDB_ROOT_USERNAME=admin -e MONGO_INITDB_ROOT_PASSWORD=password mongo
```

#### 2. Enter mongodb terminal

```
docker exec -it mongo bash
mongosh -u admin -p password
```

#### 3. Create database and user

```
use orleans_image_generator;
use admin;
db.createUser({user:"root",pwd:"password",roles:[{role:"dbOwner",db:"orleans_image_generator"}]});
```
