---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2022-04-22
--	Who: Markus H
--	Details:
--	Allow null display_name on engineering_unit
---------------------------------------------------------------------

ALTER TABLE public.engineering_unit
ALTER COLUMN display_name DROP NOT NULL; 

-- Remove limit on import log message and file_list
ALTER TABLE public.import_log
ALTER COLUMN file_list TYPE character varying; 

ALTER TABLE public.import_log_message
ALTER COLUMN message TYPE character varying; 

ALTER TABLE public.import_log_warning
ALTER COLUMN message TYPE character varying; 
