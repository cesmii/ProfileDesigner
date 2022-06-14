import axios from 'axios'
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService';

const CLASS_NAME = "AxiosService";

//-------------------------------------------------------------------
// Region: Common / Helper Profile Methods
//-------------------------------------------------------------------
const axiosInstance = axios.create({
	baseURL: `${AppSettings.BASE_API_URL}/`
});

//-------------------------------------------------------------------
//  TBD - is this the best place for this? 
//  Add Auth token on every http call to our API, set the authorization token in the header.
//  if not present, set to null
//  TBD - only set the header when API call is to our base url...
//-------------------------------------------------------------------
axiosInstance.interceptors.request.use(
	config => {

		let token = localStorage.getItem("authTicket")
			? JSON.parse(localStorage.getItem("authTicket")).token
			: null;

		if (token == null) return config;
		//append token to header if present. Some requests like login do not require token.
		//everything else does.
		config.headers.authorization = `Bearer ${token}`;
		return config;
	},
	error => {
		console.log(generateLogMessageString(`axiosInstance.interceptors.request||error`, CLASS_NAME));
		return Promise.reject(error);
	}
);


export default axiosInstance