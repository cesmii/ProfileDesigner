# Profile Designer

## Prerequisites

- Install node.js (version > 10.16) - https://nodejs.org/en/
- Install npm (version > 5.6) - https://www.npmjs.com/ (note I just upgraded to 7.17 =>  npm install -g npm)
- React - https://reactjs.org/ - First time React users, install React using create-react-app from a node.js command prompt, a tool that installs all of the dependencies to build and run a full React.js application.
- .NET Core 6.0.x, Visual Studio 2022 or equivalent
- DB - Install PostgreSQL
  - Install PostgreSql, including pgAdmin (https://www.enterprisedb.com/downloads/postgres-postgresql-downloads)
  - See below for more details on setting up DB for this project. 

## Directories
- \api - This contains a .NET web API back end for profile designer. Within this solution, the OPC translations will occur, database connections will occur, etc. 
- \frontend - This contains the REACT front end for profile designer.
- \SampleNodeSets - This contains nodesets that we use to import into system. Any OPC UA compliant nodeset is permitted. These are stored just for convenience while developing within the system.
- \sql - This contains the SQL script used to generate the DB structure and insert required lookup data as well as some sample users.  

## How to Build
- Clone the repo from GIT.

If you have an older clone that is missing the common submodule:
```ps
cd c:\sources\cesmii\profiledesigner
git submodule add https://github.com/cesmii/cesmii-common
```

- **Build/Run the front end:**
  ```powershell
  cd \frontend
  npm install
  npm run start 
  ```
  - Verify the site is running in a browser: http://localhost:3000

> **Note**
>
> The ENV files in the root React folder point to the base URL for the web API.  
> Login: The login process was intentionally simple. Use cesmii/cesmii to login.  



- **Build/Run the back end API (.NET 6 Solution):**
  - Standard .NET build and run. 

- **PostgreSql DB**  
	See above for initial install instructions.
	- Run pgAdmin
	- Create local DB (see AppSettings.json for database name.)
	- Open the Query Tool (Tools menu)
	- Open the CESMII-Profile-Designer\sql\CESMII.ProfileDesigner.DB.sql file
	- Create the cesmii role/login and the database (comment out everything except the create role and create database sections and run the script)
	- Open a query tool on the newly created database and run the rest of the script
	- Change the password on the cesmii login to match the one in the appsettings.development.json file.

## Run Profile Designer, Market Place and Cloud Library locally

### Profile Designer: Front End port 3002, API port 5004

1. Add a frontend/.env.development file with the following entries/content:
```
REACT_APP_BASE_API_URL=https://localhost:5004/api
PORT=3002
```

2. Change 
