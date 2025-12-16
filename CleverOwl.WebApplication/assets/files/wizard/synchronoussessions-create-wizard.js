//== Class definition
var SynchronoussessionsWizard = function () {
    //== Base elements
    var wizardEl = $('#m_wizard');
    var btnNext = wizardEl.find('[data-wizard-action="next"]');
    var btnPrev = wizardEl.find('[data-wizard-action="prev"]');
    var btnComplete = wizardEl.find('[data-wizard-action="submit"]');
    var dataName = "";

    var formEl;
    var currentStep;
    var currentUrl;
    var validator;
    var wizard;
    var dataValue;
    var minutesFromLastSMS = 0;

    function refreshCurrentStep() {

        currentStep = wizardEl.find('.m-wizard__form-step--current');

        formEl = $('#' + currentStep.data('form-id'));

        currentUrl = currentStep.data('url');

        validator = formEl.validate({
        });

        if (dataValue != null) {

            $("input[name='" + dataName + "']").val(dataValue);
        }
    }

    var initWizard = function () {
        //== Initialize form wizard
        wizard = new mWizard('m_wizard', {
            startStep: 1
        });

        //== Validation before going to next page
        wizard.on('beforeNext', function (wizardObj) {
            refreshCurrentStep();

            wizardObj.stop();

            mApp.block(wizardEl);
            mApp.progress(btnNext);

            if (!validator.form()) {

                mApp.unblock(wizardEl);
                mApp.unprogress(btnNext);


                mUtil.scrollTop();
            }

        });

        //== Change event
        wizard.on('didChange', function (wizard) {

        });
    }

    var initButtons = function () {

        btnNext.on('click', function (e) {
            e.preventDefault();

            if (!validator.form()) {
                return;
            }

            formEl.ajaxSubmit({
                url: currentUrl,
                beforeSubmit: function () {
                },
                statusCode: {
                    412: function () {
                        if (wizard.getStep() === 1) {
                            getConflictSwal();
                        }
                    }
                },
                success: function (data) {
                    if (wizard.getStep() == 1) {
                        $('#bar_1').removeClass('wizard-step-bar-current');
                        $('#bar_1').addClass('wizard-step-bar-done');
                        $('#bar_2').removeClass('wizard-step-bar');
                        $('#bar_2').addClass('wizard-step-bar-current');
                    }
                    if (wizard.getStep() == 2) {
                        $('#bar_2').removeClass('wizard-step-bar-current');
                        $('#bar_2').addClass('wizard-step-bar-done');
                        $('#bar_2').removeClass('wizard-step-bar');

                        $('#bar_3').removeClass('wizard-step-bar');
                        $('#bar_3').addClass('wizard-step-bar-current');
                    }
                    console.log("getStep");
                    console.log(wizard.getStep());
                    if (wizard.getStep() === 2 && formEl[0].id === "sessionAudienceForm") {
                        console.log("Step 2");
                        $('#m_wizard_form_step_3').html(data);
                    }

                    mApp.unblock(wizardEl);
                    mApp.unprogress(btnNext);

                    wizard.start();
                    wizard.skimNext();

                    mUtil.scrollTop();

                },
                error: function (xhr, status, error) {

                    mApp.unblock(wizardEl);
                    mApp.unprogress(btnNext);
                    //swal(SynchronousSessionsWizardAlertErrorTitleSwal, SynchronousSessionsWizardAlertErrorTextSwal, "error");

                }
            });
        });

        btnPrev.on('click', function (e) {
            if (wizard.getStep() == 1) {
                $('#bar_1').addClass('wizard-step-bar-current');
                $('#bar_1').removeClass('m-wizard__step-bar-done');
                $('#bar_2').addClass('wizard-step-bar');
                $('#bar_2').removeClass('wizard-step-bar-current');
            }
            if (wizard.getStep() == 2) {
                $('#bar_2').addClass('wizard-step-bar-current');
                $('#bar_2').removeClass('m-wizard__step-bar-done');
                //$('#bar_2').addClass('wizard-step-bar');

                $('#bar_3').addClass('wizard-step-bar');
                $('#bar_3').removeClass('wizard-step-bar-current');
            }

        });

        btnComplete.on('click', function (e) {
            e.preventDefault();

            refreshCurrentStep();

            if (!validator.form()) {
                return;
            }

            mApp.block(wizardEl);
            mApp.progress(btnComplete);

            formEl.ajaxSubmit({
                url: currentUrl,
                beforeSubmit: function () {
                },
                statusCode: {
                    412: function () {
                    }
                },
                success: function (data) {

                    mApp.unblock(wizardEl);
                    mApp.unprogress(btnComplete);

                    swal({
                        allowOutsideClick: false,
                        reverseButtons: true,
                        "title": SynchronousSessionsWizardAlertSuccessTitleSwal,
                        "text": SynchronousSessionsWizardAlertSuccessTextSwal,
                        "type": "success",
                        "confirmButtonClass": "btn m-btn--pill btn-brand",
                        "confirmButtonText": SynchronousSessionsWizardAlertConfirmButtonSwal,
                        showCancelButton: true,
                        "cancelButtonClass": "btn m-btn--pill btn-metal",
                        "cancelButtonText": SynchronousSessionsWizardAlertCopyLinkButtonSwal
                    }).then((result) => {
                        if (result.value) {
                            mApp.block(wizardEl);
                            mApp.progress(btnComplete);
                            window.location = successUrl;
                        }
                        else if (result.dismiss === Swal.DismissReason.cancel) {

                            navigator.clipboard.writeText(data.MeetingUrl).then(function () {
                            }, function () {
                                console.log('clipboard failed');
                            });

                            swal.fire({
                                "title": SynchronousSessionsWizardAlertCopiedTitleSwal,
                                "text": SynchronousSessionsWizardAlertCopiedTextSwal,
                                "type": 'success',
                                "confirmButtonClass": "btn m-btn--pill btn-brand",
                                "confirmButtonText": SynchronousSessionsWizardAlertConfirmButtonSwal
                            }).then((result) => {
                                if (result.value) {
                                    mApp.block(wizardEl);
                                    mApp.progress(btnComplete);
                                    window.location = successUrl;
                                }
                            });
                        }
                    });
                },
                error: function (xhr, status, error) {

                    mApp.unblock(wizardEl);
                    mApp.unprogress(btnComplete);

                    swal(SynchronousSessionsWizardSubmitAlertErrorTitleSwal, SynchronousSessionsWizardSubmitAlertErrorTextSwal, "error");

                }
            });
        });
    }

    return {
        // public functions
        init: function () {
            wizardEl = $('#m_wizard');
            formEl = $('#m_form');

            initWizard();
            initButtons();

        },
        triggerNext: function () {

            if (wizard.getStep() === 2) {
                btnComplete.trigger("click");
            }
            else {
                btnNext.trigger("click");
            }
        }
    };
}();

jQuery(document).ready(function () {
    SynchronoussessionsWizard.init();
});