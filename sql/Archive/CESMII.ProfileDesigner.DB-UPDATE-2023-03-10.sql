	---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-03-15
--	Who: Paul Yao
--	Details:
--	Store self-service sign-up details
---------------------------------------------------------------------

	ALTER TABLE public.user
	ADD COLUMN sssu_organization character varying (128) NULL,
	ADD COLUMN sssu_cesmii_member boolean NULL;