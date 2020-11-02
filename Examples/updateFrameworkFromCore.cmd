REM Copy common from BookingSystem.AspNetCore into BookingSystem.AspNetFramework
REM This helps keep the two examples in sync

copy .\BookingSystem.AspNetCore\IdComponents\*.* .\BookingSystem.AspNetFramework\IdComponents\
copy .\BookingSystem.AspNetCore\Stores\*.* .\BookingSystem.AspNetFramework\Stores\
copy .\BookingSystem.AspNetCore\Feeds\*.* .\BookingSystem.AspNetFramework\Feeds\
copy .\BookingSystem.AspNetCore\Settings\*.* .\BookingSystem.AspNetFramework\Settings\
copy .\BookingSystem.AspNetCore\Extensions\*.* .\BookingSystem.AspNetFramework\Extensions\
pause