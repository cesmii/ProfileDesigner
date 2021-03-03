import React from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import App from './App';
import { AuthContextProvider } from "./components/authentication/AuthContext";
import { LoadingContextProvider } from "./components/contexts/LoadingContext";
import reportWebVitals from './reportWebVitals';

require('dotenv').config()

//var express = require('express');
//var server = express();
//var options = {
//    index: 'index.html'
//};
//server.use('/', express.static('/home/site/wwwroot', options));
//server.listen(process.env.PORT);

ReactDOM.render(
  <React.StrictMode>
    <AuthContextProvider>  {/*When the context within this is null or false, user can't get to private routes. When true, they can*/}
        <LoadingContextProvider>
            <App />
        </LoadingContextProvider>
    </AuthContextProvider>
  </React.StrictMode>,
  document.getElementById('root')
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();

