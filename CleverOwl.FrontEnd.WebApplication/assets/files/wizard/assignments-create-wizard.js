//== Class definition
var AssignmentWizard = function () {
    //== Base elements
    var wizardEl = $('#m_wizard');
    var btnNext = wizardEl.find('[data-wizard-action="next"]');
    var btnPrev = wizardEl.find('[data-wizard-action="prev"]');
    var btnComplete = wizardEl.find('[data-wizard-action="submit"]');
    var dataName = "PropertyKey";
    var formEl;
    var currentStep;
    var currentUrl;
    var validator;
    var wizard;
    var dataValue;
    var minutesFromLastSMS = 0;

    function refreshCurrentStep() {
        //currentStep = wizardEl.find('.m-wizard__form-step--current');
        //formEl = $('#' + currentStep.data('data-form-id'));
        //currentUrl = currentStep.data('url');
        //validator = formEl.validate({
        //    //== Validate only visible fields
        //    ignore: ":hidden"
        //});
        //if (dataValue != null) {
        //    $("input[name='" + dataName + "']").val(dataValue);
        //}

        //wizard.start();
        if (wizard.getStep() == 1) {
            $('#assignmentNameHelp').hide();
            $('#descriptionHelp').hide();
            $('#pageImageDropZoneHelp').hide();
            var assigName = $('#assignmentName').val();
            var messageData = btoa($('#description').summernote('code'));
            var file = fileGeted;
            if (assigName == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                var position = $('.m-wizard__form').offset().top;
                $('html, body').animate({
                    scrollTop: position
                }, 1000);
                $('#assignmentNameHelp').show();
                wizard.stop();
            }
            if (messageData == '' || messageData == 'PHA+PGJyPjwvcD4=') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                var position = $('.m-wizard__form').offset().top;
                $('html, body').animate({
                    scrollTop: position
                }, 1000);
                $('#descriptionHelp').show();
                wizard.stop();
            }
            if (file == null) {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                var position = $('.m-wizard__form').offset().top;
                $('html, body').animate({
                    scrollTop: position
                }, 1000);
                $('#pageImageDropZoneHelp').show();
                wizard.stop();
            }
        }
        if (wizard.getStep() == 2) {
            $('#submissionDateHelp').hide();
            $('#dueDateHelp').hide();
            $('#reminderDateHelp').hide();
            var submissionDate = $('#submissionDate').val();
            var dueDate = $('#dueDate').val();
            var reminderDate = $('#reminderDate').val();
            if (submissionDate == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                var position = $('.m-wizard__form').offset().top;
                $('html, body').animate({
                    scrollTop: position
                }, 1000);
                $('#submissionDateHelp').show();
                wizard.stop();
            }
            if (dueDate == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                var position = $('.m-wizard__form').offset().top;
                $('html, body').animate({
                    scrollTop: position
                }, 1000);
                $('#dueDateHelp').show();
                wizard.stop();
            }
            if (reminderDate == '') {
                mApp.unprogress(btnNext);
                mApp.unblock(wizardEl);
                var position = $('.m-wizard__form').offset().top;
                $('html, body').animate({
                    scrollTop: position
                }, 1000);
                $('#reminderDateHelp').show();
                wizard.stop();
            }

        }
        if (wizard.getStep() == 3) {

        }
        if (wizard.getStep() == 4) {

        }
    }

    var initWizard = function () {
        //== Initialize form wizard
        wizard = new mWizard('m_wizard', {
            startStep: 1
        });

        //== Validation before going to next page
        wizard.on('beforeNext', function (wizardObj) {
            mApp.block(wizardEl);
            mApp.progress(btnNext);
            refreshCurrentStep();
            //if (!validator.form()) {
            //    console.log("not validate");
            //}
                //mApp.unblock(wizardEl);
                //mApp.unprogress(btnNext);
                //mUtil.scrollTop();

        });

        //== Change event
        wizard.on('change', function (wizardObj) {
            mApp.unprogress(btnNext);
            mApp.unblock(wizardEl);
            var position = $('.m-wizard__form').offset().top;
            $('html, body').animate({
                scrollTop: position
            }, 1000);
            //wizard.start();
        });
    }

    var initButtons = function () {

        btnNext.on('click', function (e) {
            //e.preventDefault();

            //if (!validator.form()) {
            //    return;
            //}
            


        });
        btnPrev.on('click', function (e) {
            
            
        });

        btnComplete.on('click', function (e) {
            
            
            //e.preventDefault();

            refreshCurrentStep();

            //if (!validator.form()) {
            //    return;
            //}

            mApp.block(wizardEl);
            mApp.progress(btnComplete);

            //formEl.ajaxSubmit({
            //    url: currentUrl,
            //    beforeSubmit: function () {
            //    },
            //    statusCode: {
            //        412: function () {
            //            if (wizard.getStep() === 4) {
            //                swal(CrowdContentParticipateAlertContributionCategoriesMissingTitleSwal, CrowdContentParticipateAlertContributionCategoriesMissingTextSwal, "warning");
            //            }
            //        }
            //    },
            //    success: function (data) {

            //        mApp.unblock(wizardEl);
            //        mApp.unprogress(btnComplete);

            //        swal({
            //            "title": CrowdContentParticipateAlertSuccessTitleSwal,
            //            "text": CrowdContentParticipateAlertSuccessTextSwal,
            //            "type": "success",
            //            "confirmButtonClass": "btn btn-secondary m-btn m-btn--wide"
            //        }).then((result) => {
            //            if (result.value) {
            //                window.location = successUrl;
            //            }
            //        });
            //    },
            //    error: function (xhr, status, error) {

            //        mApp.unblock(wizardEl);
            //        mApp.unprogress(btnComplete);

            //        swal(CrowdContentParticipateSubmitAlertErrorTitleSwal, CrowdContentParticipateSubmitAlertErrorTextSwal, "error");

            //    }
            //});
        });
    }

    return {
        // public functions
        init: function () {
            wizardEl = $('#m_wizard');
            formEl = $('#m_form');
            initWizard();
            initButtons();

        }
    };
}();

jQuery(document).ready(function () {
    
    AssignmentWizard.init();
});