---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-07-07
--	Who: MarkusH
--	Details:
--	Profile created, updated, is_active columns,
---------------------------------------------------------------------

ALTER TABLE public.profile ADD COLUMN created timestamp with time zone NULL;
ALTER TABLE public.profile ADD COLUMN created_by_id integer NULL;
ALTER TABLE public.profile ADD COLUMN updated timestamp with time zone NULL;
ALTER TABLE public.profile ADD COLUMN updated_by_id integer NULL;
ALTER TABLE public.profile ADD COLUMN is_active boolean NULL;

UPDATE public.profile SET created = (select min(created) from public.profile_type_definition where profile.id = id);
UPDATE public.profile SET created = to_timestamp(0) where created is null;

UPDATE public.profile SET created_by_id = owner_id;
UPDATE public.profile SET created_by_id = 1 where created_by_id is null;

UPDATE public.profile SET updated = (select min(updated) from public.profile_type_definition where profile.id = id);
UPDATE public.profile SET updated = to_timestamp(0) where updated is null;

UPDATE public.profile SET updated_by_id = owner_id;
UPDATE public.profile SET updated_by_id = 1 where updated_by_id is null;

ALTER TABLE public.profile ALTER COLUMN created SET NOT NULL;
ALTER TABLE public.profile ALTER COLUMN created_by_id SET NOT NULL;
ALTER TABLE public.profile ALTER COLUMN updated SET NOT NULL;
ALTER TABLE public.profile ALTER COLUMN updated_by_id SET NOT NULL;


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