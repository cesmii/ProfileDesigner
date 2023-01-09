---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-01-04
--	Who: Paul Y
--	Details:
--	Adding email to user list
---------------------------------------------------------------------
ALTER TABLE public.user
    ADD email_address character varying(320) COLLATE pg_catalog."default" NULL;
