angular.module('warrantyApp').factory('ClaimsService', function ($http) {
    return {
        list: function () { return $http.get(API_BASE + '/claims').then(r => r.data); },
        search: function (q) { return $http.get(API_BASE + '/claims/search', { params: { q: q } }).then(r => r.data); },
        get: function (id) { return $http.get(API_BASE + '/claims/' + id).then(r => r.data); },
        create: function (payload) { return $http.post(API_BASE + '/claims', payload).then(r => r.data); },
        submit: function (id) { return $http.post(API_BASE + '/claims/' + id + '/submit', {}).then(r => r.data); },
        approve: function (id) { return $http.post(API_BASE + '/claims/' + id + '/approve', {}).then(r => r.data); },
        reject: function (id, reason) { return $http.post(API_BASE + '/claims/' + id + '/reject', { reason: reason }).then(r => r.data); },
        markInvoiced: function (id) { return $http.post(API_BASE + '/claims/' + id + '/mark-invoiced', {}).then(r => r.data); },
        manufacturers: function () { return $http.get(API_BASE + '/manufacturers').then(r => r.data); }
    };
});
