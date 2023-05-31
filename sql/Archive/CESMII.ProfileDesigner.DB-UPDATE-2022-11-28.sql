---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2022-11-28
--	Who: MarkusH
--	Details:
--	CloudLib Import
---------------------------------------------------------------------
ALTER  TABLE public.standard_nodeset
    ALTER COLUMN version DROP NOT NULL,
    ALTER COLUMN publish_date TYPE timestamp with time zone,
    ADD COLUMN cloudlibrary_id character varying(25) COLLATE  pg_catalog."default" NOT NULL -- may require manual data update
;

ALTER TABLE public."nodeset_file"
ALTER COLUMN publish_date TYPE timestamp with time zone
;

ALTER TABLE public."profile"
ALTER COLUMN publish_date TYPE timestamp with time zone,
 -- Cloud Library meta data
ADD COLUMN title text NULL,
ADD COLUMN license text NULL,
ADD COLUMN copyright_text text NULL,
ADD COLUMN contributor_name text NULL,
ADD COLUMN description text NULL,
ADD COLUMN category_name text NULL,
ADD COLUMN documentation_url text NULL,
ADD COLUMN icon_url text NULL,
ADD COLUMN license_url text NULL,
ADD COLUMN keywords text[] NULL,
ADD COLUMN purchasing_information_url text NULL,
ADD COLUMN release_notes_url text NULL,
ADD COLUMN test_specification_url text NULL,
ADD COLUMN supported_locales text[] NULL
	-- End Cloud Library meta data
;

CREATE TABLE public.profile_additional_properties
(
    id SERIAL PRIMARY KEY,
    profile_id integer NOT NULL,
	name text NOT NULL,
	value text NULL,
	CONSTRAINT profile_id_fk FOREIGN KEY (profile_id)
		REFERENCES public.profile (id) MATCH SIMPLE
)
TABLESPACE pg_default;

ALTER TABLE public.profile_additional_properties
    OWNER to profiledesigner;

drop procedure if exists public.sp_nodeset_delete;
create procedure public.sp_nodeset_delete(
   _idList varchar
)
language plpgsql    
as $$
begin
	--
	delete from public.profile_type_definition_user_analytics
	--SELECT * FROM public.profile_type_definition_user_analytics
	WHERE profile_type_definition_id in (
		SELECT ptd.id 
		FROM public.profile_type_definition ptd 
		JOIN public.profile p on p.id = ptd.profile_id
			AND p.id IN (select cast(unnest(string_to_array(_idList, ',')) as int)) 
	);

	--
	delete from public.profile_type_definition_user_favorite
	--SELECT * FROM public.profile_type_definition_user_favorite
	WHERE profile_type_definition_id in (
		SELECT ptd.id 
		FROM public.profile_type_definition ptd 
		JOIN public.profile p on p.id = ptd.profile_id
			AND p.id IN (select cast(unnest(string_to_array(_idList, ',')) as int)) 
	);

	--
	delete from public.profile_attribute
	--SELECT * FROM public.profile_attribute
	WHERE profile_type_definition_id in (
		SELECT ptd.id 
		FROM public.profile_type_definition ptd 
		JOIN public.profile p on p.id = ptd.profile_id
			AND p.id IN (select cast(unnest(string_to_array(_idList, ',')) as int)) 
	);

	delete from public.data_type
	--SELECT * FROM public.data_type
	WHERE custom_type_id in (
		SELECT ptd.id 
		FROM public.profile_type_definition ptd 
		JOIN public.profile p on p.id = ptd.profile_id
			AND p.id IN (select cast(unnest(string_to_array(_idList, ',')) as int))
	);

	delete from public.profile_composition
	--SELECT * FROM public.profile_composition
	WHERE profile_type_definition_id in (
		SELECT ptd.id 
		FROM public.profile_type_definition ptd 
		JOIN public.profile p on p.id = ptd.profile_id
			AND p.id IN (select cast(unnest(string_to_array(_idList, ',')) as int))
	);

	delete from public.profile_interface
	--SELECT * FROM public.profile_interface
	WHERE profile_type_definition_id in (
		SELECT ptd.id 
		FROM public.profile_type_definition ptd
		JOIN public.profile p on p.id = ptd.profile_id
			AND p.id IN (select cast(unnest(string_to_array(_idList, ',')) as int))
	);

	delete from public.profile_type_definition
	--SELECT * FROM public.profile_type_definition
	WHERE profile_id in (
		SELECT id FROM public.profile p
		WHERE p.id IN (select cast(unnest(string_to_array(_idList, ',')) as int))
	);

	delete from public.profile_nodeset_file
	--SELECT * FROM public.profile_nodeset_file
	WHERE profile_id in (
		SELECT id FROM public.profile p
		WHERE p.id IN (select cast(unnest(string_to_array(_idList, ',')) as int))
	);

  	delete from public.nodeset_file f
	--SELECT * FROM public.nodeset_file f
	WHERE NOT EXISTS (
		SELECT id FROM public.profile_nodeset_file pf
		WHERE f.id = pf.nodeset_file_id
	);

	delete from public.import_log_warning
	--SELECT * FROM public.import_log_warning
	WHERE profile_id in (
		SELECT id FROM public.profile p
		WHERE p.id IN (select cast(unnest(string_to_array(_idList, ',')) as int))
	);

	delete from public.profile_additional_properties
	--SELECT * FROM public.profile
	WHERE profile_id IN (select cast(unnest(string_to_array(_idList, ',')) as int))
	;

	delete from public.profile
	--SELECT * FROM public.profile
	WHERE id IN (select cast(unnest(string_to_array(_idList, ',')) as int))
	;
    commit;
end;$$

