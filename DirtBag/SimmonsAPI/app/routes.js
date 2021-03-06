var User            = require('./models/user');  // get our mongoose model
var express         = require('express');
var jwt             = require('jsonwebtoken'); // used to create, sign, and verify tokens

module.exports = function(app){

  app.get('/setup', function(req, res){

    // create sample user
    var bob = new User({
      name: 'Bob Dole',
      created: new Date(),
      admin: false,
      lastLogin: new Date(),
      active: true,
      password: 'chickennoodlesoup',
    });

    // save the sample user
    bob.save(function(err){
      if (err){throw err;};

      console.log('User saved successfully');
      res.json({success: true});
    })
  });


  // ROUTES FOR API
  // =======================================================================================================
  var router = express.Router();// get an instance of the express router

  // middleware to use for all requests
  router.use(function(req, res, next) {
      // do logging
      console.log('Something is happening.');
      next(); //make sure we go to the next routes and dont stop here
  });

  // route to authenticate user (POST http://localhost:8087/api/authenticate)
  router.post('/authenticate', function(req, res){

    // find user
    User.findOne({
      name: req.body.name
    }, function(err, user){
      if(err) throw err;
      if(!user){
        res.json({success: false, message: 'Authentication failed. User not found.'});
      } else if (user){
        // check if password matches
        if(user.password != req.body.password){
          res.json({success: false, message: "Authentication failed. Wrong password"});
        } else {
          // if user is found and password is right, create tokens
          var token = jwt.sign(user, app.get('superSecret'), {
            expiresInMinutes: 1440 // expires in 24 hours
          });

          // return the information including token as json
          res.json({
            success: true,
            message: "Enjoy your token!",
            token: token
          });
        }
      }
    })
  })


  // route middleware to verify a token
  router.use(function(req, res, next){
    //check header or url parameters or post parameters for token
    var token = req.body.token || req.query.token || req.headers['x-access-token'];

    //decode token
    if (token) {

      // verifies secret and checks exp
      jwt.verify(token, app.get('superSecret'), function(err, decoded){
        if(err){
          return res.json({ success: false, message: 'Failed to authenticate token.'});
        } else {
          // if everything is good, save to request for use in other routes
          req.decoded = decoded;
          next();
        }
      });
    } else {
      // if there is no token, return an errors
      return res.status(403).send({
        sucess: false,
        message: 'No token provided.'
      })
    }
  });


  // test route to make sure everything is working (accessed at GET htttp://localhost:8080/api
  router.get('/', function (req, res) {
    res.send('Hello! The API is at http://localhost:' + port + '/api');
      // res.json({ message: 'hooray! welcome to our api!' });
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
      })

      // delete user with this id (accessed at DELETE http://localhost:8087/api/users/:user_id)
      .delete(function(req, res){
        User.remove({
          _id: req.params.user_id
        }, function(err, user){
          if(err){
            res.send(err);
          };
          res.json({message: 'Successfully deleted.'})
        });
      });


  // REGISTER ROUTES
  // =======================================================================================================

  // app.get('*', function(req, res){
  //   res.sendfile('./public/views/index.html'); // loud our public/index.html file
  // });

  app.use('/api', router);
};
