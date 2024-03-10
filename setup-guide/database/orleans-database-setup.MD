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

## Copy all sql files

docker cp ./01-MySQL-Main.sql database-db-1:/01-MySQL-Main.sql
docker cp ./02-MySQL-Clustering.sql database-db-1:/02-MySQL-Clustering.sql
docker cp ./03-MySQL-Persistence.sql database-db-1:/03-MySQL-Persistence.sql

## Run SQL files on database docker instance

### 1. connect to terminal 

```sh
docker exec -it database-db-1 bash
```

### 2. open sql bash for the database

```sh
mysql -u root -p orleans_image_generator
```

### 3. It will prompt for password

type in the password and press enter

### 4. execute the sql scripts in sequence

```sh
source 01-MySQL-Main.sql
```

```sh
source 02-MySQL-Clustering.sql
```

```sh
source 03-MySQL-Persistence.sql
```

## verification of tables and stored-procedures

### 1. run the command to show all tables

```sh
show tables;
```

it should show 4 tables:

```
mysql> show tables;
+-----------------------------------+
| Tables_in_orleans_image_generator |
+-----------------------------------+
| OrleansMembershipTable            |
| OrleansMembershipVersionTable     |
| OrleansQuery                      |
| OrleansStorage                    |
+-----------------------------------+
4 rows in set (0.01 sec)
```

### 2. run the commands to check the procedures


```sh
SELECT ROUTINE_NAME 
FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_TYPE="PROCEDURE" 
AND ROUTINE_SCHEMA="orleans_image_generator"
AND ROUTINE_NAME="ClearStorage";
```

Expected output:

```
mysql> SELECT ROUTINE_NAME 
    -> FROM INFORMATION_SCHEMA.ROUTINES 
    -> WHERE ROUTINE_TYPE="PROCEDURE" 
    -> AND ROUTINE_SCHEMA="orleans_image_generator"
    -> AND ROUTINE_NAME="ClearStorage";
+--------------+
| ROUTINE_NAME |
+--------------+
| ClearStorage |
+--------------+
1 row in set (0.01 sec)
```

```sh
SELECT ROUTINE_NAME 
FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_TYPE="PROCEDURE" 
AND ROUTINE_SCHEMA="orleans_image_generator"
AND ROUTINE_NAME="InsertMembershipKey";
```

Expected Output:

```
mysql> SELECT ROUTINE_NAME 
    -> FROM INFORMATION_SCHEMA.ROUTINES 
    -> WHERE ROUTINE_TYPE="PROCEDURE" 
    -> AND ROUTINE_SCHEMA="orleans_image_generator"
    -> AND ROUTINE_NAME="InsertMembershipKey";
+---------------------+
| ROUTINE_NAME        |
+---------------------+
| InsertMembershipKey |
+---------------------+
1 row in set (0.01 sec)
```


```sh
SELECT ROUTINE_NAME 
FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_TYPE="PROCEDURE" 
AND ROUTINE_SCHEMA="orleans_image_generator"
AND ROUTINE_NAME="WriteToStorage";
```

Expected Output:

```
mysql> SELECT ROUTINE_NAME 
    -> FROM INFORMATION_SCHEMA.ROUTINES 
    -> WHERE ROUTINE_TYPE="PROCEDURE" 
    -> AND ROUTINE_SCHEMA="orleans_image_generator"
    -> AND ROUTINE_NAME="WriteToStorage";
+----------------+
| ROUTINE_NAME   |
+----------------+
| WriteToStorage |
+----------------+
1 row in set (0.02 sec)
```

### 3. run the commands to check the indexes

- Validate the Index on OrleansMembershipVersionTable

```sh
SELECT 'OrleansMembershipVersionTable.PRIMARY' AS 'IndexName', EXISTS (    SELECT 1    FROM INFORMATION_SCHEMA.STATISTICS     WHERE TABLE_SCHEMA = 'orleans_image_generator' AND TABLE_NAME = 'OrleansMembershipVersionTable'    AND INDEX_NAME = 'PRIMARY') AS 'IndexExists';
```

#### Expected Output:

```
mysql> SELECT 'OrleansMembershipVersionTable.PRIMARY' AS 'IndexName', EXISTS (    SELECT 1    FROM INFORMATION_SCHEMA.STATISTICS     WHERE TABLE_SCHEMA = 'orleans_image_generator'     AND TABLE_NAME = 'OrleansMembershipVersionTable'    AND INDEX_NAME = 'PRIMARY') AS 'IndexExists';
+---------------------------------------+-------------+
| IndexName                             | IndexExists |
+---------------------------------------+-------------+
| OrleansMembershipVersionTable.PRIMARY |           1 |
+---------------------------------------+-------------+
1 row in set (0.00 sec)
```

- Validate the Index on OrleansQuery

```sh
SELECT 'OrleansQuery.PRIMARY' AS 'IndexName', EXISTS (    SELECT 1    FROM INFORMATION_SCHEMA.STATISTICS     WHERE TABLE_SCHEMA = 'orleans_image_generator' AND TABLE_NAME = 'OrleansQuery' AND INDEX_NAME = 'PRIMARY') AS 'IndexExists';
```

#### Expected Output:

```
mysql> SELECT 'OrleansQuery.PRIMARY' AS 'IndexName', EXISTS (    SELECT 1    FROM INFORMATION_SCHEMA.STATISTICS     WHERE TABLE_SCHEMA = 'orleans_image_generator' AND TABLE_NAME = 'OrleansQuery' AND INDEX_NAME = 'PRIMARY') AS 'IndexExists';
+---------------------------------------+-------------+
| IndexName                             | IndexExists |
+---------------------------------------+-------------+
| OrleansQuery.PRIMARY |           1 |
+---------------------------------------+-------------+
1 row in set (0.01 sec)
```

- Validate the Index on OrleansStorage


```sh
SELECT 'OrleansStorage.PRIMARY' AS 'IndexName', EXISTS (    SELECT 1    FROM INFORMATION_SCHEMA.STATISTICS     WHERE TABLE_SCHEMA = 'orleans_image_generator' AND TABLE_NAME = 'OrleansStorage' AND INDEX_NAME = 'IX_OrleansStorage') AS 'IndexExists';
```

#### Expected Output:


```
mysql> SELECT 'OrleansStorage.PRIMARY' AS 'IndexName', EXISTS (    SELECT 1    FROM INFORMATION_SCHEMA.STATISTICS     WHERE TABLE_SCHEMA = 'orleans_image_generator' AND TABLE_NAME = 'OrleansStorage' AND INDEX_NAME = 'IX_OrleansStorage') AS 'IndexExists';
+---------------------------------------+-------------+
| IndexName                             | IndexExists |
+---------------------------------------+-------------+
| OrleansStorage.PRIMARY |           1 |
+---------------------------------------+-------------+
1 row in set (0.00 sec)
```


- Validate the Index on OrleansMembershipTable

```sh
SELECT 'OrleansMembershipTable.PRIMARY' AS 'IndexName', EXISTS (    SELECT 1    FROM INFORMATION_SCHEMA.STATISTICS     WHERE TABLE_SCHEMA = 'orleans_image_generator' AND TABLE_NAME = 'OrleansMembershipTable' AND INDEX_NAME = 'PRIMARY') AS 'IndexExists';
```

#### Expected Output:


```
mysql> SELECT 'OrleansMembershipTable.PRIMARY' AS 'IndexName', EXISTS (    SELECT 1    FROM INFORMATION_SCHEMA.STATISTICS     WHERE TABLE_SCHEMA = 'orleans_image_generator' AND TABLE_NAME = 'OrleansMembershipTable' AND INDEX_NAME = 'PRIMARY') AS 'IndexExists';
+--------------------------------+-------------+
| IndexName                      | IndexExists |
+--------------------------------+-------------+
| OrleansMembershipTable.PRIMARY |           1 |
+--------------------------------+-------------+
1 row in set (0.01 sec)
```
