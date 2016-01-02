// server.js

// BASE SETUP
// ======================================================================================================

// call the packages we need
var express = require('express');// call express
var app = express();// define our app using express
var bodyParser = require('body-parser');
var User = require('./app/models/user');

// configure app to use bodyParser()
// this will let us get data from a POST
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.json());

var port = process.env.PORT || 8087;// set our port

var mongoose = require('mongoose');
mongoose.connect('mongodb://localhost:27017/Simmons');
var db = mongoose.connection;



// ROUTES FOR API
// =======================================================================================================
var router = express.Router();// get an instance of the express router

// middleware to use for all requests
router.use(function(req, res, next) {
    // do logging
    console.log('Something is happening.');
    next(); //make sure we go to the next routes and dont stop here
});

// test route to make sure everything is working (accessed at GET htttp://localhost:8080/api
router.get('/', function (req, res) {
    res.json({ message: 'hooray! welcome to our api!' });
});

// more routes for SimmonsAPI go here
router.route('/users')
    // create a user (accessed at POST http://localhost:1337/api/users
    .post(function(req, res) {
        var user = new User(); // create a new instance of the user model
        user.userName = req.body.userName; // set the users name (comes from the request)
        user.accountCreatedDate = req.body.accountCreatedDate;
        user.isAdmin = req.body.isAdmin;
        user.lastLogin = req.body.lastLogin;

        // save the user and check for errors
        user.save(function(err) {
            if (err) {
                res.send(err);
                console.log(err);
                return;
            };
            res.json({ message: 'User created!' });
        });
    })
    //get all users (accessed at GET http://localhost:1337/api/users
    .get(function(req, res) {
        User.find(function(err, users) {
            if (err) {
                res.send(err);
            };
            res.json(users);
        });
    });


router.route('/users/:user_id')

    // get user with that id (accessed at GET http://localhost:1337/api/users/:user_id)
    .get(function (req, res) {
        User.findById(req.params.user_id, function(err, user) {
            if (err) {
                res.send(err);
            };
            res.json(user);
        });
    })

    // update user with this id (accessed at PUT http://localhost:8087/api/users/:user_id)
    .put(function(req, res){

      // use user model to find user we want
      User.findById(req.params.user_id, function(err, user){

        if(err){
          res.send(err);
        }

        user.userName = req.body.userName;  // update users info

        // save the user
        user.save(function(err){
          if(err){
            res.send(err);
          };

          res.json({message: 'User updated!'});
        });

      });

    });



// REGISTER ROUTES
// =======================================================================================================

app.use('/api', router);

// START THE SERVER
// =======================================================================================================
app.listen(port);
console.log('magic happens on port ' + port);
