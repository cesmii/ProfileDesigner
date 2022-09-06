---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2022-08-24
--	Who: Markus H
--	Details:
--	Add columns for import/export fidelity
---------------------------------------------------------------------

ALTER TABLE public.profile_attribute
	ADD COLUMN eng_unit_access_level integer NULL,
	ADD COLUMN eu_range_access_level integer NULL,
	ADD COLUMN minimum_sampling_interval numeric NULL;
