

//== Class definition
var CrowdContentParticipateWizard = function () {
    //== Base elements
    var wizardEl = $('#m_wizard');
    var btnNext = wizardEl.find('[data-wizard-action="next"]');
    var btnComplete = wizardEl.find('[data-wizard-action="submit"]');
    var dataName = $('#canvelBtn');
    var formEl;
    var currentStep;
    var currentUrl;
    var validator;
    var wizard;
    var dataValue;
    var minutesFromLastSMS = 0;
    var currentWizardStep;
    function refreshCurrentStep() {

        currentStep = wizardEl.find('.m-wizard__form-step--current');
        formEl = $('#' + currentStep.data('form-id'));

        currentUrl = currentStep.data('url');

        validator = formEl.validate({
            //== Validate only visible fields
            ignore: ":hidden"
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
       // initContentContributionSelects();

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
        wizard.on('DidChange', function (wizard) {
            
            if (wizard.getStep() != 1) {
                $('#btnNextContent').text(CrowdContentParticipatePageContinue);
            }

            if (wizard.getStep() == 2) {
            }
            if (wizard.getStep() === 3) {

                populatePhoneNumber();

                if (minutesFromLastSMS <= 5) {

                }
                else {
                    resetTimer(60);
                }
            }
            if (wizard.getStep() === 4) {

                //initContentContributionSelects();

            }
        });
    }

    var initButtons = function () {

        btnNext.on('click', function (e) {
            e.preventDefault();
            if (!validator.form()) {
                console.log('form not validate');
                return;
            }

            
            

            formEl.ajaxSubmit({
                url: currentUrl,
                beforeSubmit: function () {
                },
                statusCode: {
                    412: function () {
                        if (wizard.getStep() === 2) {
                            swal(CrowdContentParticipateAlertAcceptTermsTitleSwal, CrowdContentParticipateAlertAcceptTermsTextSwal, "info");
                        }
                        if (wizard.getStep() === 3) {
                            swal(CrowdContentParticipateAlertWrongCodeTitleSwal, CrowdContentParticipateAlertWrongCodeBodySwal, "warning");
                        }
                    }
                },
                success: function (data) {
                  
                    if (data == "AcknowledgementNotFound") {
                        console.log("AcknowledgementNotFound");
                        $("#" + data).text(noAckknolegmentFound);
                        mApp.unblock(wizardEl);
                        mApp.unprogress(btnNext);

                    } else if (data == "AcknowledgementNotFound2"){
                        console.log("AcknowledgementNotFound2");
                        $("#" + data).text(noAckknolegmentFound);
                        mApp.unblock(wizardEl);
                        mApp.unprogress(btnNext); 
                    } else {
                        console.log('success');
                        console.log(data);
                        //if (dataValue === 0 && wizard.getStep() === 1) {
                            
                        //    //populate data on step 1
                        //}
                        if (wizard.getStep() == 1) {
                            $('#cancelBtn').show();
                            
                        }
                        if (wizard.getStep() == 2) {
                            
                        }

                        minutesFromLastSMS = data.MinutesFromLastSMS;

                        //if (wizard.getStep() === 3) {

                        //    phoneVerified();
                        //}
                        mApp.unblock(wizardEl);
                        mApp.unprogress(btnNext);

                        wizard.start();
                        wizard.skimNext();

                        mUtil.scrollTop();
                        
                    }
                    

                },
                error: function (xhr, status, error) {
                    
                    mApp.unblock(wizardEl);
                    mApp.unprogress(btnNext);

                    swal('Empty Field!', 'a field is empty, please check again', "error");

                }
            });
        });

        //btnComplete.on('click', function (e) {
        //    e.preventDefault();

        //    refreshCurrentStep();

        //    participationLastStep = wizard.getStep();

        //    if (!validator.form()) {
        //        return;
        //    }

        //    mApp.block(wizardEl);
        //    mApp.progress(btnComplete);

        //    formEl.ajaxSubmit({
        //        url: currentUrl,
        //        beforeSubmit: function () {
        //        },
        //        statusCode: {
        //            412: function () {
        //                if (wizard.getStep() === 4) {
        //                    swal(CrowdContentParticipateAlertContributionCategoriesMissingTitleSwal, CrowdContentParticipateAlertContributionCategoriesMissingTextSwal, "warning");
        //                }
        //            }
        //        },
        //        success: function (data) {

        //            mApp.unblock(wizardEl);
        //            mApp.unprogress(btnComplete);

        //            swal({
        //                "title": CrowdContentParticipateAlertSuccessTitleSwal,
        //                "text": CrowdContentParticipateAlertSuccessTextSwal,
        //                "type": "success",
        //                "confirmButtonClass": "btn btn-secondary m-btn m-btn--wide"
        //            }).then((result) => {
        //                if (result.value) {
        //                    window.location = successUrl;
        //                }
        //            });
        //        },
        //        error: function (xhr, status, error) {

        //            mApp.unblock(wizardEl);
        //            mApp.unprogress(btnComplete);

        //            swal('CrowdContentParticipateSubmitAlertErrorTitleSwal', 'CrowdContentParticipateSubmitAlertErrorTextSwal', "error");

        //        }
        //    });
        //});
        
    }

    return {
        // public functions
        init: function () {
            wizardEl = $('#m_wizard');
            formEl = $('#m_form');

            initWizard();
            initButtons();

            //if (dataPropertyKey != null) {
            //    dataValue = dataPropertyKey;
            //}
            if (participationLastStep != 0 && participationLastStep != 5) {

                swal({
                    "title": CrowdContentParticipateGreetBackSuccessTitleSwal,
                    "html": CrowdContentParticipateGreetBackSuccessBodySwal,
                    "type": "success",
                    "confirmButtonClass": "btn btn-secondary m-btn m-btn--wide"
                }).then((result) => {
                    if (result.value) {
                        refreshCurrentStep();
                        wizard.skimTo(participationLastStep);
                    }
                });

            }
        },
        triggerNext: function () {

            if (wizard.getStep() === 4) {
                btnComplete.trigger("click");
            }
            else {
                btnNext.trigger("click");
            }
        }
    };
}();

jQuery(document).ready(function () {
    CrowdContentParticipateWizard.init();
});