﻿(function () {
    'use strict';

    // create controller 
    function DeleteController($scope, userGroupsResource, navigationService, treeService, notificationsService) {

        $scope.delete = function (id) {
            userGroupsResource.deleteGroup(id)
                .then(function (resp) {
                    if (resp.status === 200) {
                        notificationsService.success('SUCCESS', resp.data);
                        refreshTree();
                    } else {
                        notificationsService.error('ERROR', resp.data);
                    }

                    navigationService.hideNavigation();
                });
        };

        $scope.cancelDelete = function () {
            navigationService.hideNavigation();
        };

        function refreshTree() {
            treeService.loadNodeChildren({ node: $scope.$parent.currentNode.parent(), section: 'users' })
                .then(function (r) {
                    window.location = '/umbraco/#/workflow';
                });
        }

    };

    // register controller 
    angular.module('umbraco').controller('Workflow.UserGroups.Delete.Controller', DeleteController);
}());
