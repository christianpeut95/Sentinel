# Create a collection mapping to create new database entities from survey submission

Follow [Create a simple survey field mapping](https://sentinelsurveillance.app/docs/?page=create-a-simple-survey-field-mapping) first if not done already.
This guide demonstrates how to set up Sentinel Survey mapping to create new patient, contact, or exposure records from submitted survey data. Business rules from the simple mapping you create will apply (e.g review first before creation). There are additional options for duplicate detection to avoid the creation of duplicate patient records, not demonstrated in this guide

*49 steps · Created Oct 23, 2026 · Source: demo.sentinelsurveillance.app*

---

## Step 01: In the disease edit page, expand the assigned task template and click Add Mapping

![Step 01](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-01.webp)

## Step 02: Enter a mapping name and select a survey question

Once Collection mapping is configured, the simple survey-question selection is not used. Only non-matrix or non-dynamic questions are currently listed here; select one to continue.

![Step 02](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-02.webp)

## Step 03: Select the business rules and set Mapping Complexity to Collection

![Step 03](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-03.webp)

## Step 04: Click Save Mapping

![Step 04](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-04.webp)

## Step 05: Back in the task template, click the edit icon for the mapping just created

![Step 05](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-05.webp)

## Step 06: In the mapping editor, click Configure

![Step 06](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-06.webp)

## Step 07: Review the Collection Mapping configuration window

![Step 07](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-07.webp)

## Step 08: In Step 1, select the matrix or dynamic question to use

![Step 08](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-08.webp)

## Step 09: Check the selected question in the Matrix Question field

![Step 09](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-09.webp)

## Step 10: Select Patient as the Primary Entity Type

The primary entity is the patient record created from each survey row.

![Step 10](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-10.webp)

## Step 11: In Step 2, click Add Mapping to map survey columns to patient fields

Fields for related case, contact or exposure records are configured later.

![Step 11](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-11.webp)

## Step 12: Select the Source Column containing the first name

![Step 12](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-12.webp)

## Step 13: In Target Field, select First Name

![Step 13](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-13.webp)

## Step 14: Check the target field and set Required or a Default Value as appropriate

![Step 14](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-14.webp)

## Step 15: Add the next mapping and select the Source Column containing the last name

![Step 15](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-15.webp)

## Step 16: In Target Field, select Last Name

![Step 16](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-16.webp)

## Step 17: Continue mapping the remaining patient fields

The Required boxes are not selected in this screenshot. Select them where required or provide a default value.

![Step 17](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-17.webp)

## Step 18: In Step 3, add an entity related to the patient

Cases and contacts both use the Case entity.

![Step 18](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-18.webp)

## Step 19: For this example, select Contact as the related entity type

![Step 19](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-19.webp)

## Step 20: Under the related entity, click Add Mapping

![Step 20](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-20.webp)

## Step 21: Choose the Source Type for the related-entity mapping

Column uses survey data; Context uses data about the survey or its case, such as DiseaseId; Primary uses the patient record just created; Related Entity uses an entity created earlier; Constant uses a fixed value.

![Step 21](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-21.webp)

## Step 22: Select Primary to use the new patient record as the source

![Step 22](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-22.webp)

## Step 23: In Source, select Id from the primary patient record

![Step 23](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-23.webp)

## Step 24: In Target Field, select PatientId

![Step 24](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-24.webp)

## Step 25: Click Add Mapping to add the disease mapping

![Step 25](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-25.webp)

## Step 26: For this row, set Source Type to Context

![Step 26](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-26.webp)

## Step 27: Open the Source menu to select a context field

![Step 27](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-27.webp)

## Step 28: Select DiseaseId and map it to the related record’s DiseaseId

This is the disease of the case or contact for which the survey is being completed.

![Step 28](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-28.webp)

## Step 29: Add a Constant mapping with value 2 to the contact record’s Type field

In this mapping, 2 is the value for Contact.

![Step 29](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-29.webp)

## Step 30: Add an ExposureEvent as the next related entity

![Step 30](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-30.webp)

## Step 31: Under the ExposureEvent entity, click Add Mapping

![Step 31](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-31.webp)

## Step 32: Set Source Type to Related Entity to use the contact just created

![Step 32](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-32.webp)

## Step 33: Select the contact entity and its Id field as the source

![Step 33](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-33.webp)

## Step 34: In Target Field, select ExposedCaseId

![Step 34](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-34.webp)

## Step 35: Click Add Mapping to add the source-case mapping

![Step 35](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-35.webp)

## Step 36: For this row, set Source Type to Context

![Step 36](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-36.webp)

## Step 37: In Source, select CaseId

![Step 37](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-37.webp)

## Step 38: In Target Field, select SourceCaseId

![Step 38](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-38.webp)

## Step 39: Click Add Mapping to add the contact-classification mapping

![Step 39](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-39.webp)

## Step 40: Set Source Type to Constant and enter the ContactClassificationId value

![Step 40](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-40.webp)

## Step 41: At the bottom of the collection window, click Save Configuration

![Step 41](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-41.webp)

## Step 42: Back in the mapping editor, click Save Mapping

![Step 42](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-42.webp)

## Step 43: Open a sample survey to verify the mapping

![Step 43](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-43.webp)

## Step 44: In the matrix or dynamic question, click Add Row

![Step 44](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-44.webp)

## Step 45: Enter sample data in the new row

![Step 45](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-45.webp)

## Step 46: At the end of the survey, click Complete

![Step 46](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-46.webp)

## Step 47: Click Complete at the top of the page

![Step 47](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-47.webp)

## Step 48: Check that the patient, contact and exposure records have been created

![Step 48](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-48.webp)

## Step 49: Open the record details to review the mapped values

![Step 49](images/create-a-collection-mapping-to-create-new-database-entities-from-survey-submission/step-49.webp)
