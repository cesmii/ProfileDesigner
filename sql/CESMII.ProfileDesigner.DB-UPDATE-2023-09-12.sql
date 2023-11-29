---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-07-07
--	Who: MarkusH
--	Details:
--	ProfileAttribute 
---------------------------------------------------------------------

ALTER TABLE public.profile_attribute ADD COLUMN allow_sub_types boolean NULL;

