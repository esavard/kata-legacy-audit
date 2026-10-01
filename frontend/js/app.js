// app.js - route config and the API base URL, hardcoded, because environment-specific
// config was "a problem for later" during the 2019 Azure port. Switching between local
// and Azure means hand-editing this file - see README.md.
var API_BASE = 'http://localhost:5000/api';

angular.module('warrantyApp', ['ngRoute'])
    .config(function ($routeProvider) {
        $routeProvider
            .when('/login', { templateUrl: 'views/login.html', controller: 'LoginController', controllerAs: 'vm' })
            .when('/claims', { templateUrl: 'views/claims-list.html', controller: 'ClaimsListController', controllerAs: 'vm' })
            .when('/claims/new', { templateUrl: 'views/new-claim.html', controller: 'NewClaimController', controllerAs: 'vm' })
            .when('/claims/:id', { templateUrl: 'views/claim-detail.html', controller: 'ClaimDetailController', controllerAs: 'vm' })
            .when('/queue', { templateUrl: 'views/claims-list.html', controller: 'ClaimsListController', controllerAs: 'vm' })
            .otherwise({ redirectTo: '/login' });
    });
