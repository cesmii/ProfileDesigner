# Cloud Library Mock

The mock relies on a brute-force data recorder: you run the tests on a dev machine and record the accessed patterns and responses. If either the tests or the cloud library changes, the data needs to be re-recorded.

The data is stored in the CloudLibMock/Searchdata.json file, which also gets copied to the output folder.
## Re-Record Mock Data

1) delete the CloudLibMock/Searchdata.json file in both the project directory and the bin\Debug\net6.0\CloudLibMock output directory.
2) Setup a cloud library that matches the expected profiles, ideally the library that is created by running the cloud library tests on an empty/non-existent database.
3) Configure profile designer to use the cloud library, for example by adding the following to the user's secrets.json
```json
{
  "CloudLibrary": {
    "UserName": "test@test.com",
    "Password": "test",
    "EndPoint": "https://localhost:5001"
  }
}
```
4) Run all the Profile Designer test cases against an empty PD database, or one that has equivalent profiles from the ones created by the test cases.
5) After the test run, copy the bin\Debug\net6.0\CloudLibMock\Searchdata.json to the project's \CloudLibMock folder. To verify, rerun the tests with the recorded data.
6) Check in the new Searchdata.json and verify that the tests on the GitHub runner pass as well.
