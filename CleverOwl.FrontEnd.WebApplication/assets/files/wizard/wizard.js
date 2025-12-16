//== Class definition
var RecordMatchingWizard = function () {
    //== Base elements
    var wizardEl = $('#m_wizard');
    var formEl = $('#m_form');
    var matchesDatatable = $('#EmployeeMatchesTable');
    var generateNewEmployeeNumberSwitch = $('#NewEmployeeNumberSwitch input[type="checkbox"]');
    var proceedButton = $('#proceedButton');
    var printButton = $('#printButton');
    var validator;
    var wizard;
    
    //== Private functions


    function generateEmployeeNumber(wizardObj) {

        wizardObj.stop();

        $.ajax({
            type: 'POST',
            url: generateEmployeeNumberUrl,
            data: generateEmployeeNumberData,
            beforeSend: function (data) {

                mApp.progress(proceedButton);
                mApp.block(formEl);

            },
            success: function (data) {

                mApp.unprogress(proceedButton);
                mApp.unblock(formEl);

                wizardObj.skimNext();

                generateCodes(data.PersonalNumber, data.EmployeeRecordKey);
                printButton.show();
                fillQRIndexData(generateEmployeeNumberData.FirstName, generateEmployeeNumberData.SurName, generateEmployeeNumberData.FatherName, generateEmployeeNumberData.MotherName);
            },
            error: function () {

                swal("Error", "We couldn't generate a new employee number, please try again.", "error");

                mApp.unprogress(proceedButton);
                mApp.unblock(formEl);

            }
        });
    }

    var initWizard = function () {
        //== Initialize form wizard
        wizard = new mWizard('m_wizard', {
            startStep: 1
        });

        //== Validation before going to next page
        wizard.on('beforeNext', function (wizardObj) {

            var newEmployeeNumber = generateNewEmployeeNumberSwitch.is(':checked');
            var selectedRow = matchesDatatable.DataTable().row('.selected').data();

            if (!selectedRow && !newEmployeeNumber) {

                wizardObj.stop();

                swal("No Employee", "You have to select an employee before proceeding. Alternatively you can generate a new Employee Number.", "warning");

            } else {

                if (newEmployeeNumber) {
                    generateEmployeeNumber(wizardObj);
                }
                else if (selectedRow) {
                    generateCodes(selectedRow[1].replace('<b>', '').replace('</b>', ''), selectedRow[0].replace('<b>', '').replace('</b>', ''));
                    printButton.show();
                    fillQRIndexData(selectedRow[2], selectedRow[3], selectedRow[4], selectedRow[5]);
                }
            }

            if (validator.form() !== true) {

                wizardObj.stop();

            }
        })

        wizard.on('beforePrev', function (wizardObj) {

            wizardObj.stop();
        })

        //== Change event
        wizard.on('change', function(wizard) {
            mUtil.scrollTop();            
        });

        //== Change event
        wizard.on('change', function(wizard) {
            if (wizard.getStep() === 2) {
            }           
        });
    }

    var initValidation = function() {
        validator = formEl.validate({
            //== Validate only visible fields
            ignore: ":hidden",
            
            //== Display error  
            invalidHandler: function(event, validator) {     
                mUtil.scrollTop();

                swal({
                    "title": "", 
                    "text": "There are some errors in your submission. Please correct them.", 
                    "type": "error",
                    "confirmButtonClass": "btn btn-secondary m-btn m-btn--wide"
                });
            },

            //== Submit valid form
            submitHandler: function (form) {
                
            }
        });   
    }

    var initSubmit = function () {

        var btn = formEl.find('[data-wizard-action="submit"]');

        btn.on('click', function (e) {
            e.preventDefault();

            if (validator.form()) {

                if (printButton.data('printedOnce') != 1) {
                    swal("Info", "You have to print the labels first.", "info");
                    return;
                }


                $('#finishLabelingFormRecordKey').val(usedRecordKey);

                mApp.progress(btn);
                mApp.block(formEl); 

                $('#finishLabelingForm').ajaxSubmit({
                    success: function () {

                        window.location = labelingIndexUrl;

                    },
                    error: function () {

                        swal("Error", "We couldn't complete labeling for this employee, please try again.", "error");

                        mApp.unprogress(btn);
                        mApp.unblock(formEl);

                    }
                });
            }
        });
    }

    return {
        // public functions
        init: function() {
            wizardEl = $('#m_wizard');
            formEl = $('#m_form');

            initWizard(); 
            initValidation();
            initSubmit();
        }
    };
}();

jQuery(document).ready(function() {    
    RecordMatchingWizard.init();
});