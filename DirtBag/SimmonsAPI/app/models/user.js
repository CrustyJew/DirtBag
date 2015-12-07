// app/models/user.js

var mongoose    = require('mongoose');
var Schema = mongoose.Schema;

var UserSchema = new Schema({
    userName: String,
    accountCreatedDate: Date,
    isAdmin: Boolean,
    lastLogin: Date,
    isActive: Boolean
});

module.exports = mongoose.model('User', UserSchema);