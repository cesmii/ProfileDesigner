---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2022-09-20
--	Who: Sean C
--	Details:
--	Migrate to Azure AD - update user table, drop permissions table
---------------------------------------------------------------------

-- DROP TABLE public."user";
ALTER TABLE public."user"
ADD objectid_aad character varying(100) COLLATE pg_catalog."default" NULL, 
ADD display_name character varying(250) COLLATE pg_catalog."default" NULL
;

--TBD - insert rows using object ids from AAD (static list provided) and maaping to pre-defined targets
UPDATE public."user" 
SET objectid_aad = 'dcfd0710-d766-4705-8e04-5cd8afa639ed'
	,display_name = 'David Wickman'
WHERE username='davidw';	
UPDATE public."user" 
SET objectid_aad = 'c79a4d42-8c58-42f4-9b32-3ddb9ce4fd79'
	,display_name = 'Sean Coxen'
WHERE username='seanc';	
UPDATE public."user" 
SET objectid_aad = '1f25a66e-cb03-467e-a164-14ba265d472a'
	,display_name = 'Markus Horstmann'
WHERE username='markush';	
--
UPDATE public."user" 
SET objectid_aad = 'faa49db5-807b-4fa3-bb3c-2e1298712c95'
	,display_name = 'info@cesmii.org'
WHERE username='cesmii';	
UPDATE public."user" 
SET objectid_aad = '14a8a203-9581-4873-8fda-9569b85af853'
	,display_name = 'Olivia Morales'
WHERE username='oliviam';	
UPDATE public."user" 
SET objectid_aad = '694b9d2c-7503-4230-8ce9-6f46ec7d97a4'
	,display_name = 'Jonathan Wise'
WHERE username='jonathanw';	
UPDATE public."user" 
SET objectid_aad = 'ea0b03ea-10fe-4e0a-bddf-457c12bcff6a'
	,display_name = 'Chris Meunch'
WHERE username='chrism';	
UPDATE public."user" 
SET objectid_aad = 'e0b4e084-2e67-46f4-97d0-769ecd5bba56'
	,display_name = 'Doug Lawson'
WHERE username='thinkiq';	
UPDATE public."user" 
SET objectid_aad = '76ea8ea9-9006-461d-b982-5dcce82d710e'
	,display_name = 'Prakashan Korambath'
WHERE username='korambath';	


--set not null for objectIdAAD
ALTER TABLE public."user"
ALTER COLUMN objectid_aad character varying(100) COLLATE pg_catalog."default" NOT NULL
;
--preserve this for now until cutover is successful, however, make it nullable so we can 
--add new users dynamically w/o passwords. Passwords will live in Azure AD.
ALTER TABLE public."user"
ALTER COLUMN password DROP NOT NULL
;
ALTER TABLE public."user"
ALTER COLUMN username DROP NOT NULL
;
ALTER TABLE public."user"
ALTER COLUMN first_name DROP NOT NULL
;
ALTER TABLE public."user"
ALTER COLUMN last_name DROP NOT NULL
;
ALTER TABLE public."user"
ALTER COLUMN email DROP NOT NULL
;
ALTER TABLE public."user"
ALTER COLUMN is_active set default true;
;

--drop obsolete cols
ALTER TABLE public."user"
DROP CONSTRAINT user_username_key;

DROP INDEX user_username_6821ab7c_like;

ALTER TABLE public."user"
--DROP COlUMN password,
DROP COlUMN first_name,
DROP COlUMN last_name,
DROP COlUMN registration_complete,
;
--DROP COlUMN username,

--drop permissions data
DROP TABLE public."user_permission";
DROP TABLE public."permission";

