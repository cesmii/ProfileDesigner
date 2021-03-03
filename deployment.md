<h1>Profile Designer</h1>
<h2>Prerequisites</h2>
<ul>
<li>
	Install node.js (version > 10.16) - https://nodejs.org/en/
</li>
<li>
	Install npm (version > 5.6) - https://www.npmjs.com/
</li>
<li>
	React - https://reactjs.org/
</li>
</ul>

<h2>Directories</h2>
<ul>
<li>
	\mock-api - This contains a mock API back end for profile designer. It uses a package called JSON-server (https://www.npmjs.com/package/json-server). 
</li>
<li>
	\mock-api\data - Most of the data used by the site is stored in this file. There is some additional lookup static data stored within the REACT code base. 
</li>
<li>
	\smart-web - This contains the REACT front end for profile designer.
</li>
</ul>

<h2>How to Build</h2>
<ol>
<li>
	Clone the repo from GIT.
</li>
<li>
	<b>Build/Run the mock-api (Using a node.js prompt): </b>
	<ul>
		<li>
			cd \mock-api
		</li>
		<li>
			npm intstall
		</li>
		<li>
			npm run startdev - this will run the mock api locally. 
		</li>
		<li>
			Verify the site is running in a browser: http://localhost:3001
		</li>
	</ul>
	<p>
	Note: npm run start - this can be used to run the mock api in a deployed setting
	</p>
</li>
<li>
	<b>Build/Run the front end (Using a node.js prompt): </b>
	<ul>
		<li>
			cd \smart-web
		</li>
		<li>
			npm intstall
		</li>
		<li>
			npm run start 
		</li>
		<li>
			Verify the site is running in a browser: http://localhost:3000
		</li>
	</ul>
	<p>
	Notes: In order to use the site, the mock-api must be running. 
	Login: For phase I, the login process was intentionally simple. User data is stored in the db.json file and the login only checks that submitted password matches user name. Use cesmii/cesmii to login. 
	</p>
</ol>



