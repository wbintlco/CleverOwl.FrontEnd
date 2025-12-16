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
    var nextClicked = false;
    var backClicked = false;
    function refreshCurrentStep() {
        if (wizard.getStep() == 1) {
            
            $('form[id="second_form"]').submit();
            //wizard.stop();
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
            var position = $('.head').offset().top - 80;
            $('html, body').animate({
                scrollTop: position
            }, 1000);
            //wizard.start();
        });
    }

    var initButtons = function () {

        btnNext.on('click', function (e) {

            if (wizard.getStep() == 2) {
                $('#bar_1').removeClass('bar-current');
                $('#bar_1').addClass('bar-done');
                $('#bar_2').removeClass('bar');
                $('#bar_2').addClass('bar-current');
            }
            if (wizard.getStep() == 3) {
                $('#bar_2').removeClass('bar-current');
                $('#bar_2').addClass('bar-done');
                $('#bar_3').removeClass('bar');
                $('#bar_3').addClass('bar-current');
            }
            if (wizard.getStep() == 4) {
                $('#bar_3').removeClass('bar-current');
                $('#bar_3').addClass('bar-done');
                $('#bar_4').removeClass('bar');
                $('#bar_4').addClass('bar-current');
            }

        });
        btnPrev.on('click', function (e) {
            if (wizard.getStep() == 3) {
                $('#bar_4').removeClass('bar-current');
                $('#bar_4').addClass('bar');
                $('#bar_3').removeClass('bar-done');
                $('#bar_3').addClass('bar-current');
            }
            if (wizard.getStep() == 2) {
                $('#bar_3').removeClass('bar-current');
                $('#bar_3').addClass('bar');
                $('#bar_2').removeClass('bar-done');
                $('#bar_2').addClass('bar-current');
            }
            if (wizard.getStep() == 1) {
                $('#bar_2').removeClass('bar-current');
                $('#bar_2').addClass('bar');
                $('#bar_1').removeClass('bar-done');
                $('#bar_1').addClass('bar-current');
            }

        });

        btnComplete.on('click', function (e) {


            //e.preventDefault();

            refreshCurrentStep();

            //if (!validator.form()) {
            //    return;
            //}

            //mApp.block(wizardEl);
            //mApp.progress(btnComplete);

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
