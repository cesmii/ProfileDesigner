---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-07-07
--	Who: MarkusH
--	Details:
--	Profile created, updated, is_modified columns,
---------------------------------------------------------------------

ALTER TABLE public.profile ADD COLUMN created timestamp with time zone NULL;
UPDATE public.profile SET created = (select min(created) from public.profile_type_definition where profile.id = id)
UPDATE public.profile SET created = to_timestamp(0) where created is null
ALTER TABLE public.profile ALTER COLUMN created SET NOT NULL;

ALTER TABLE public.profile ADD COLUMN created_by_id integer NULL;
UPDATE public.profile SET created_by_id = owner_id;
UPDATE public.profile SET created_by_id = 1 where created_by_id is null
ALTER TABLE public.profile ALTER COLUMN created_by_id SET NOT NULL;

ALTER TABLE public.profile ADD COLUMN updated timestamp with time zone NOT NULL;
ALTER TABLE public.profile ADD COLUMN updated_by_id integer NOT NULL;
ALTER TABLE public.profile ADD COLUMN is_active boolean NULL;

ALTER TABLE public.profile
    ADD CONSTRAINT profile_created_by_id FOREIGN KEY (created_by_id)
        REFERENCES public.user (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        DEFERRABLE INITIALLY DEFERRED;
ALTER TABLE public.profile
    ADD CONSTRAINT profile_updated_by_id FOREIGN KEY (updated_by_id)
        REFERENCES public.user (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        DEFERRABLE INITIALLY DEFERRED;