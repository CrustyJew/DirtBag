// server.js

// BASE SETUP
// ======================================================================================================

// call the packages we need
var express = require('express');                               // call express
var app = express();                                        // define our app using express
var bodyParser = require('body-parser');
var User = require('./app/models/user');

// configure app to use bodyParser()
// this will let us get data from a POST
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.json());

var port = process.env.PORT || 8087;                                    // set our port

var mongoose = require('mongoose');
mongoose.connect('localhost:27017');
var db = mongoose.connect;



// ROUTES FOR API
// =======================================================================================================
var router = express.Router();                                          // get an instance of the express router

// test route to make sure everything is working (accessed at GET htttp://localhost:8080/api
router.get('/', function (req, res) {
    res.json({ message: 'hooray! welcome to our api!' });
});

// more routes for SimmonsAPI go here

// REGISTER ROUTES
// =======================================================================================================

app.use('/api', router);

// START THE SERVER
// =======================================================================================================
app.listen(port);
console.log('magic happens on port ' + port);