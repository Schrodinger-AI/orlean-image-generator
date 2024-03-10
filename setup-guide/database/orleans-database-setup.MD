# Orleans Database Setup

Orleans framework need tables and procedures to manage the Grain Operations
Execute the steps listed below:

## MySQL Setup

- You are in project root directory
- Change to directory with database setup files

```sh
cd setup-guide/database/
```

- Start mysql docker container

```sh
docker-compose up -d
```

#### Copy all sql files

docker cp ./01-MySQL-Main.sql database-db-1:/01-MySQL-Main.sql
docker cp ./02-MySQL-Clustering.sql database-db-1:/02-MySQL-Clustering.sql
docker cp ./03-MySQL-Persistence.sql database-db-1:/03-MySQL-Persistence.sql

#### Run SQL files on database docker instance

1. connect to terminal 

```sh
docker exec -it database-db-1 bash
```

2. open sql bash for the database

```sh
mysql -u root -p orleans_image_generator
```

3. It will prompt for password

type in the password and press enter

4. execute the sql scripts

```sh
source 01-MySQL-Main.sql
```

```sh
source 02-MySQL-Clustering.sql
```

```sh
source 03-MySQL-Persistence.sql
```
