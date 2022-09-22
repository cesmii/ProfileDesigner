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

--set not null for objectIdAAD
ALTER TABLE public."user"
ALTER COLUMN objectid_aad character varying(100) COLLATE pg_catalog."default" NOT NULL
;

--drop obsolete cols
ALTER TABLE public."user"
DROP CONSTRAINT user_username_key;

DROP INDEX user_username_6821ab7c_like;

ALTER TABLE public."user"
DROP COlUMN password,
DROP COlUMN username,
DROP COlUMN first_name,
DROP COlUMN last_name,
DROP COlUMN registration_complete,
;

--drop permissions data
DROP TABLE public."user_permission";
DROP TABLE public."permission";
