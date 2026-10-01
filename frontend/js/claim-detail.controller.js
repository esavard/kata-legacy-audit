angular.module('warrantyApp').controller('ClaimDetailController', function ($routeParams, $sce, ClaimsService) {
    var vm = this;
    var id = $routeParams.id;

    vm.claim = null;
    vm.trustedDescription = '';

    ClaimsService.get(id).then(function (data) {
        vm.claim = data.claim;
        vm.estimatedTotal = data.estimatedTotal;

        // $sce.trustAsHtml() on a value that came straight from the database, which
        // came straight from a dealer-typed textarea with no server-side sanitization
        // (see ClaimsController.Create()). A claim whose problem description contains
        // <img src=x onerror=...> or a <script> tag runs it for whoever opens this
        // claim - which, combined with the IDOR on GetById(), is basically anyone
        // logged in, dealer or manufacturer side.
        vm.trustedDescription = $sce.trustAsHtml(vm.claim.problemDescription || '');
    });

    vm.calcClientTotal = function () {
        // FOURTH copy of the total/tax math (see ClaimsController.cs for the other
        // three) - client-side preview only, no tax applied at all here, same gap as
        // the old kata's recalcTotal().
        if (!vm.claim) return 0;
        var c = vm.claim;
        var total = (c.part1Qty || 0) * (c.part1UnitPrice || 0)
                  + (c.part2Qty || 0) * (c.part2UnitPrice || 0)
                  + (c.part3Qty || 0) * (c.part3UnitPrice || 0)
                  + (c.part4Qty || 0) * (c.part4UnitPrice || 0)
                  + (c.part5Qty || 0) * (c.part5UnitPrice || 0);
        return total;
    };

    vm.submit = function () { ClaimsService.submit(id).then(function (d) { vm.claim = d; }); };
    vm.approve = function () { ClaimsService.approve(id).then(function (d) { vm.claim = d; }); };
    vm.reject = function () { ClaimsService.reject(id, vm.rejectionReason).then(function (d) { vm.claim = d; }); };
    vm.markInvoiced = function () { ClaimsService.markInvoiced(id).then(function (d) { vm.claim = d; }); };
});
