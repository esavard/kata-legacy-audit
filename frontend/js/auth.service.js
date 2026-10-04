// auth.service.js - the token, role, and user info are kept in plain localStorage,
// unencrypted, readable by any script running on the page (including, notably, the
// stored-XSS payload from a claim's problem description - see claim-detail.controller.js).
// An AngularJS 1.x app has no secure httpOnly-cookie option built in without extra
// backend work that was never done, so this was "the easy way" in 2019 and stayed that way.
angular.module('warrantyApp').factory('AuthService', function ($http) {
    return {
        login: function (username, password) {
            return $http.post(API_BASE + '/auth/login', { username: username, password: password })
                .then(function (res) {
                    localStorage.setItem('wc_token', res.data.token);
                    localStorage.setItem('wc_role', res.data.role);
                    localStorage.setItem('wc_fullName', res.data.fullName);
                    localStorage.setItem('wc_dealerId', res.data.dealerId);
                    return res.data;
                });
        },
        logout: function () {
            localStorage.removeItem('wc_token');
            localStorage.removeItem('wc_role');
            localStorage.removeItem('wc_fullName');
            localStorage.removeItem('wc_dealerId');
        },
        getToken: function () { return localStorage.getItem('wc_token'); },
        getRole: function () { return localStorage.getItem('wc_role'); },
        getFullName: function () { return localStorage.getItem('wc_fullName'); },
        isLoggedIn: function () { return !!localStorage.getItem('wc_token'); }
    };
})
// attaches the bearer token to every request. no interceptor-level handling of 401s -
// if a token expires (it won't for a year, see JwtTokenService.cs) the app just shows
// whatever error the API returns, unhandled.
.factory('AuthInterceptor', function () {
    return {
        request: function (config) {
            var token = localStorage.getItem('wc_token');
            if (token) {
                config.headers = config.headers || {};
                config.headers.Authorization = 'Bearer ' + token;
            }
            return config;
        }
    };
})
.config(function ($httpProvider) {
    $httpProvider.interceptors.push('AuthInterceptor');
});
