# Table of contents

* [Currency Price History App by PP](#intro)
* [Running the app](#running)
* [Backend](#backend)
* [Installing Dependencies](#running-backend-installing)
* [Starting the backend app](#running-backend-starting)
* [To run the backend api locally](#running-backend-local)
* [To run the backend api in AWS](#running-backend-aws)
* [API endpoints](#running-backend-end-points)
* [Starting the frontend app](#running-frontend)
* [Unit Tests](#unit-tests)
* [Backend](#unit-tests-backend)
* [Frontend](#unit-tests-fronend)
* [Screenshots](#screenshots)
* [Application](#screenshots-app)
* [Unit Tests - Backend](#unit-tests-backend)
* [Unit Tests - Frontend](#unit-test-frontend)

# Currency Price History App by PP
<a name="intro"/>

This is a sample app with:
* Backend - Developed as a `AWS Lambda` using `NodeJS` and `Serverless` framework.
* Frontend - Developed in `React` with `TypeScript`.



# Running the app
<a name="running"/>

## Backend
<a name="running-backend"/>

### Installing Dependencies
<a name="running-backend-installing"/>

* Install [Serverless framework](https://serverless.com/framework/docs/providers/aws/guide/installation/) - Supported versions `>=1.38.0 <2.0.0` (Tested in version `1.38.0`)

* Install NodeJs version >= 8.10
* Install npm packages - Go to the `backend` directory from the command prompt and run:

```bash
npm install
```

## Starting the backend app
<a name="running-backend-starting"/>

### To run the backend api locally
<a name="running-backend-local"/>

* Go to the `backend` directory from the command prompt and run:

```bash
# you can change port 8080 to your preferred port
serverless offline start --port 8080
```

### To run the backend api in AWS
<a name="running-backend-aws"/>

* To deploy the app to AWS: go to the `backend` directory from the command prompt and run:
```bash
serverless deploy
```

## API endpoints
<a name="running-backend-end-points"/>

| HTTP method | URI path | Sample Request | Sample Response
| ------ | ------ | ------ | ------ |
|`GET`|`/currency_exchange/calculate/profit?currency=<:currencyCode>`|`http://localhost:8080/currency_exchange/calculate/profit?currency=btc`|`{"currency":"BTC","date":"07-May-18","buy":{"time":"09:15 AM","price":34.98},"sell":{"time":"12:30 PM","price":37.01},"max_profit":2.04}`

## Starting the frontend app
<a name="running-frontend"/>

* Go to the `frontend` directory.
* Create `.env` file in the `frontend` directory. 
* Add below to `.env`. (There is a `.env.dist` which has all avaiable variables)
```env
NODE_PATH=src/
REACT_APP_API_HOST=http://localhost:8080
```
`REACT_APP_API_HOST` should be the host URL of the backend API.
* Run below from the command line
```node
npm start
```
* Visit http://localhost:3000/ to view the frontend App.

# Unit Tests
<a name="unit-tests"/>


## Backend
<a name="unit-tests-backend"/>

* Go to the `backend` directory from the command line and run:
```node
npm test
```

## Frontend
<a name="unit-tests-fronend"/>

* Go to the `frontend` directory from the command line and run:
```node
npm test
```

# Screenshots
<a name="screenshots"/>

## Application
<a name="screenshots-app"/>

![Application](https://prabhath-currency-data.s3-ap-southeast-2.amazonaws.com/screenshots/app.png)

## Unit Tests - Backend
<a name="unit-tests-backend"/>

![Unit Tests - Backend](https://prabhath-currency-data.s3-ap-southeast-2.amazonaws.com/screenshots/unittest-backend.png)

## Unit Tests - Frontend
<a name="unit-test-frontend"/>

![Unit Tests - Frontend](https://prabhath-currency-data.s3-ap-southeast-2.amazonaws.com/screenshots/unittest-frontend.png)


