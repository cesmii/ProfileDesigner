import axios from 'axios'
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService';
import { Msal_Instance } from '../index.js';

const CLASS_NAME = "AxiosService";

//-------------------------------------------------------------------
// Region: Common / Helper Profile Methods
//-------------------------------------------------------------------
export const axiosInstance = axios.create({
	baseURL: `${AppSettings.BASE_API_URL}/`
});

//-------------------------------------------------------------------
//  Add Auth token on every http call to our API, set the authorization token in the header.
//  if not present, set to null
//-------------------------------------------------------------------
axiosInstance.interceptors.request.use(
	async config => {
		//moved to the other axiosInstance so that public endpoints do not have to wait for 
		//token to be retrieved on their calls.
		const token = await getBearerToken();

		if (token == null) return config;
		//append token to header if present. Some requests like public facing pages do not require token.
		config.headers.authorization = `Bearer ${token}`;
		return config;
	},
	error => {
		console.log(generateLogMessageString(`axiosInstance.interceptors.request||error`, CLASS_NAME));
		return Promise.reject(error);
	}
);

///------------------------------------------------
///	Call MSAL framework to get the active account.
/// Retrieve current token or request refresh token 
///------------------------------------------------
async function getBearerToken() {

	const instance = Msal_Instance;

	//if user is logged in, get a refresh bearer token.
	const account = instance.getActiveAccount();
	if (account) {
		const loginRequest = {
			scopes: AppSettings.MsalScopes,
			account: account,
		};

		try {
			const response = await instance.acquireTokenSilent(loginRequest);
			return response.accessToken;
		}
		catch (err) {
			console.error(generateLogMessageString(`getBearerToken||${err}`, CLASS_NAME));
		}

	}
	return null;
}

export default axiosInstance