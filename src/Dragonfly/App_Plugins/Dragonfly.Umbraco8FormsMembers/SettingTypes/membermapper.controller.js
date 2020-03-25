angular.module("umbraco").controller("UmbracoForms.SettingTypes.MemberMapperController",
	function ($scope, $routeParams, memberPickerResource, pickerResource) {

	    if (!$scope.setting.value) {

	    } else {
	        var value = JSON.parse($scope.setting.value);
	        $scope.doctype = value.doctype;
	        $scope.nameField = value.nameField;
	        $scope.nameStaticValue = value.nameStaticValue;
	        $scope.loginField = value.loginField;
	        $scope.loginStaticValue = value.loginStaticValue;
	        $scope.emailField = value.emailField;
	        $scope.emailStaticValue = value.emailStaticValue;
	        $scope.passwordField = value.passwordField;
	        $scope.passwordStaticValue = value.passwordStaticValue;
	        $scope.properties = value.properties;
	    }

	    memberPickerResource.getAllMemberTypesWithAlias().then(function (response) {
	        $scope.memtypes = response.data;
	    });

	    pickerResource.getAllFields($routeParams.id).then(function (response) {
	        $scope.fields = response.data;
	    });

	    $scope.setMemType = function () {

	        memberPickerResource.getAllProperties($scope.memtype).then(function (response) {
	            $scope.properties = response.data;
	        });
	    };

	    $scope.setValue = function () {

	        var val = {};
	        val.memtype = $scope.memtype;
	        val.nameField = $scope.nameField;
	        val.nameStaticValue = $scope.nameStaticValue;
	        val.loginField = $scope.loginField;
	        val.loginStaticValue = $scope.loginStaticValue;
	        val.emailField = $scope.emailField;
	        val.emailStaticValue = $scope.emailStaticValue;
	        val.passwordField = $scope.passwordField;
	        val.passwordStaticValue = $scope.passwordStaticValue;
	        val.properties = $scope.properties;

	        $scope.setting.value = JSON.stringify(val);
	    };
	});