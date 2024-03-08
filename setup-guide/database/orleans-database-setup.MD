# Orleans Database Setup

Orleans framework need tables and procedures to manage the Grain Operations
Execute the steps listed below:

## MySQL Setup

```sh
docker-compose up -d
```

### Create Database entities

```sh
cd setup-guide/database/
```

#### Create OrleansQuery

```sh
mysql -uroot -p --database=orleans_image_generator < ./01-MySQL-Main.sql
```

#### Create Orleans Membership

```sh
mysql -uroot -p --database=orleans_image_generator < ./02-MySQL-Clustering.sql
```

#### Create OrleansStorage table and insert static data

```sh
mysql -uroot -p --database=orleans_image_generator < ./03-MySQL-Persistence.sql
```
