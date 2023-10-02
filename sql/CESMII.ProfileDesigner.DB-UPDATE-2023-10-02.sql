---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-10-02
--	Who: SeanC
--	Details:
--	Rename import column
---------------------------------------------------------------------

ALTER TABLE public.import_log DROP COLUMN IF EXISTS file_list;
ALTER TABLE public.import_log ADD queue_data character varying NULL;

