// app/models/user.js

var mongoose    = require('mongoose');
var Schema = mongoose.Schema;

var UserSchema = new Schema({
    name: String,
    created: Date,
    admin: Boolean,
    lastLogin: Date,
    active: Boolean,
    password: String,
});

module.exports = mongoose.model('User', UserSchema);
